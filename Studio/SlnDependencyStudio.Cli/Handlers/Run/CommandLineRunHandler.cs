using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.Exceptions;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Services;
using System.Text.Json;

namespace SlnDependencyStudio.Cli.Handlers.Run;

/// <inheritdoc cref="ICommandLineRunHandler"/>
internal sealed class CommandLineRunHandler : CommandLineHandlerBase, ICommandLineRunHandler
{
    // One step in the run pipeline. Execute returns true to continue to the next step, or false when the step
    // failed - the pipeline then returns FailureExitCode (or UserCancelled if the token fired meanwhile).
    private sealed record PipelineStep(Func<DependencyProjectDocument, CancellationToken, Task<bool>> Execute, StudioCliExitCode FailureExitCode);

    private readonly IDependencyGenerator _generator;
    private readonly IRestoreSolutionRunner _restoreSolutionRunner;
    private readonly IPreGenerationCommandRunner _preGenerationCommandRunner;
    private readonly IPostGenerationCommandRunner _postGenerationCommandRunner;
    private readonly IDependencyProjectValidator _projectValidator;
    private readonly ILogger<CommandLineRunHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineRunHandler"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="restoreSolutionRunner">The solution restore runner.</param>
    /// <param name="preGenerationCommandRunner">The pre-generation command runner.</param>
    /// <param name="postGenerationCommandRunner">The post-generation command runner.</param>
    /// <param name="projectValidator">The dependency project document validator.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineRunHandler(IDependencyProjectSerializer serializer, IDependencyGenerator generator,
        IRestoreSolutionRunner restoreSolutionRunner, IPreGenerationCommandRunner preGenerationCommandRunner,
        IPostGenerationCommandRunner postGenerationCommandRunner, IDependencyProjectValidator projectValidator,
        ILogger<CommandLineRunHandler> logger)
        : base(serializer, logger)
    {
        _generator = generator;
        _restoreSolutionRunner = restoreSolutionRunner;
        _preGenerationCommandRunner = preGenerationCommandRunner;
        _postGenerationCommandRunner = postGenerationCommandRunner;
        _projectValidator = projectValidator;
        _logger = logger;
    }

    // Cancellation is handled deliberately with two mechanisms, because the run pipeline contains two kinds of step.
    // (1) The process runners (restore, pre-gen, post-gen) report cancellation as a failed result - CommandErrorCode.Cancelled -
    // rather than throwing, so RunPipelineAsync checks cancellationToken.IsCancellationRequested around each one to tell a
    // user cancellation from a genuine step failure. (2) The serializer and generator throw OperationCanceledException, which
    // the catch below logs (with the stage, for traceability) and rethrows so App owns the exit-code mapping (UserCancelled vs
    // OperationCancelled). The pipeline's per-iteration check also covers the window where a cancellation lands after the
    // generator's last cooperative check but before it returns - so we never spawn a fresh subprocess, or exit 0, for a run
    // the user already cancelled.
    /// <inheritdoc />
    public override async Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken)
    {
        try
        {
            // Will throw DirectoryNotFoundException if the associated directory cannot be found
            var projectDirectory = GetProjectDirectory(projectFilename);

            var document = await LoadDependencyProjectDocumentAsync(projectFilename, cancellationToken).ConfigureAwait(false);

            // Log the configuration to help with troubleshooting any validation errors.
            document.LogConfiguration(projectFilename, _logger);

            // Validate all configuration up front so failures are reported before any command or
            // generation work begins. The command runners themselves do not perform validation.
            _projectValidator.Validate(document, projectDirectory);

            // Run the pipeline; it returns 0 on success or the exit code of the first step that failed or was cancelled.
            var pipelineExitCode = await RunPipelineAsync(document, cancellationToken);

            if (pipelineExitCode != 0)
            {
                return pipelineExitCode;
            }

            _logger.LogInformation("Generation complete.");

            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return (int)StudioCliExitCode.RunCommandFailed;
        }
        catch (ToolNotFoundException exception)
        {
            _logger.LogError("Required diagram tool not found: {Message}", exception.Message);
            return (int)StudioCliExitCode.DiagramToolNotFound;
        }
        catch (DiagramImageExportException exception)
        {
            _logger.LogError("Diagram image export failed: {Message}", exception.Message);
            return (int)StudioCliExitCode.DiagramImageExportFailed;
        }
        catch (ProjectAssetsException exception)
        {
            _logger.LogError("Project assets could not be read: {Message}", exception.Message);
            return (int)StudioCliExitCode.ProjectAssetsFailed;
        }
        catch (DependencyGraphException exception)
        {
            _logger.LogError("Dependency graph is inconsistent: {Message}", exception.Message);
            return (int)StudioCliExitCode.DependencyGraphFailed;
        }
        catch (DependencyGeneratorException exception)
        {
            _logger.LogError("Diagram generator failed: {Message}", exception.Message);
            return (int)StudioCliExitCode.DiagramGeneratorFailed;
        }
        catch (OperationCanceledException)
        {
            // Log the cancellation here so the log records where it originated (which stage was in
            // flight); the exit code is assigned by App, which distinguishes a user-requested shutdown
            // from an internal operation cancellation. Rethrow so the code mapping stays in one place.
            _logger.LogWarning("Operation was cancelled during generation.");
            throw;
        }
        catch (DependencyProjectException exception)
        {
            _logger.LogError("The project could not be loaded: {Message}", exception.Message);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
        catch (JsonException exception)
        {
            _logger.LogError("Failed to parse the project file. Error on line {LineNumber} for Path {Path}.", exception.LineNumber + 1, exception.Path);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
    }

    /// <summary>Runs the generation pipeline, returning <c>0</c> on success or the exit code of the first failing or cancelled step.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>0</c> when every step succeeded; otherwise the exit code of the step that failed or was cancelled.</returns>
    private async Task<int> RunPipelineAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        // Cancellation is checked before every step, so a user cancellation short-circuits before the next step runs -
        // which is what prevents the post-gen subprocess from being spawned (and a 0 exit) when a cancellation lands
        // after the generator's last cooperative check.
        PipelineStep[] pipeline =
        [
            // Execute the pre-generation command
            new(RunPreGenerationCommandIfRequiredAsync, StudioCliExitCode.PreGenerationCommandFailed),

            // Restore the solution so the dependency assets are generated
            new(RunRestoreSolutionIfRequiredAsync, StudioCliExitCode.DotNetRestoreFailed),

            // Generation is a transparent step: it throws typed exceptions / OCE on failure, so it never returns false
            // (DiagramGeneratorFailed is an unreachable fallback for the record). The per-iteration cancellation check
            // above then covers a token that fires after the generator's last cooperative check.
            new(async (doc, token) =>
            {
                await GenerateDiagramsAsync(doc, token);
                return true;
            }, StudioCliExitCode.DiagramGeneratorFailed),

            // Execute the post-generation command
            new(RunPostGenerationCommandIfRequiredAsync, StudioCliExitCode.PostGenerationCommandFailed)
        ];

        foreach (var step in pipeline)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return (int)StudioCliExitCode.UserCancelled;
            }

            if (!await step.Execute(document, cancellationToken))
            {
                // The process runners report cancellation as a failed result (CommandErrorCode.Cancelled) rather than
                // throwing OCE, so check the token again to tell a user cancellation from a genuine step failure.
                return cancellationToken.IsCancellationRequested
                    ? (int)StudioCliExitCode.UserCancelled
                    : (int)step.FailureExitCode;
            }
        }

        return 0;
    }

    /// <summary>Runs the pre-generation command if enabled. Returns <see langword="false"/> if the command failed and continue-on-failure is disabled.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if no command was required or it succeeded; <see langword="false"/> if it failed and should abort.</returns>
    private async Task<bool> RunPreGenerationCommandIfRequiredAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        var preGenConfig = document.PreGeneration;

        if (!preGenConfig.Enabled)
        {
            _logger.LogDebug("Pre-generation command disabled.");
            return true;
        }

        _logger.LogInformation("Running Pre-generation command...");

        // Subscribe to stdout/stderr so the output is captured in the CLI's log output.
        using var stdoutSub = _preGenerationCommandRunner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
        using var stderrSub = _preGenerationCommandRunner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

        // Validation ensures the command is set
        var preGenResult = await _preGenerationCommandRunner.RunAsync(preGenConfig, cancellationToken);

        if (!preGenResult.Succeeded)
        {
            // The failure itself (non-zero exit / cancelled / unexpected) is logged by the runner;
            // only the continue-on-failure policy decision is logged here.
            if (!preGenConfig.ContinueOnFailure)
            {
                _logger.LogError("Pre-generation command failed and continue-on-failure is disabled. Aborting.");

                return false;
            }

            _logger.LogWarning("Pre-generation command failed but continue-on-failure is enabled. Proceeding with generation.");
        }

        return true;
    }

    /// <summary>Restores the solution if enabled. Returns <see langword="false"/> if the restore failed and should abort.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if no restore was required or it succeeded; <see langword="false"/> if it failed and should abort.</returns>
    private async Task<bool> RunRestoreSolutionIfRequiredAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        if (!document.RestoreSolution)
        {
            _logger.LogDebug("Solution restore disabled.");
            return true;
        }

        var solutionPath = document.DiagramGenerator.Solution.SolutionPath;

        if (solutionPath.IsNullOrEmpty())
        {
            _logger.LogError("Solution restore enabled but no solution path is configured. Aborting.");
            return false;
        }

        _logger.LogInformation("Restoring solution: {SolutionPath}", solutionPath);

        // Subscribe to stdout/stderr so the output is captured in the CLI's log output.
        using var stdoutSub = _restoreSolutionRunner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
        using var stderrSub = _restoreSolutionRunner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

        var restoreResult = await _restoreSolutionRunner.RunAsync(solutionPath, cancellationToken);

        // The success/failure outcome is logged by the runner; this step only reports the decision.
        return restoreResult.Succeeded;
    }

    /// <summary>Runs the post-generation command if enabled. Returns <see langword="false"/> if the command failed.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if no command was required or it succeeded; <see langword="false"/> if it failed.</returns>
    private async Task<bool> RunPostGenerationCommandIfRequiredAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        var postGenConfig = document.PostGeneration;

        if (!postGenConfig.Enabled)
        {
            _logger.LogDebug("Post-generation command disabled.");
            return true;
        }

        _logger.LogInformation("Running Post-generation command...");

        // Subscribe to stdout/stderr so the output is captured in the CLI's log output.
        using var stdoutSub = _postGenerationCommandRunner.StdOut.Subscribe(line => _logger.LogDebug("{Line}", line));
        using var stderrSub = _postGenerationCommandRunner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

        var postGenResult = await _postGenerationCommandRunner.RunAsync(postGenConfig, cancellationToken);

        // The success/failure outcome is logged by the runner; this step only reports the decision.
        return postGenResult.Succeeded;
    }

    /// <summary>Initiates diagram generation via the dependency generator.</summary>
    /// <param name="document">The dependency project document containing the generator configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when diagram generation has finished.</returns>
    private Task GenerateDiagramsAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating diagrams...");

        return _generator.CreateDiagramsAsync(document.DiagramGenerator, cancellationToken);
    }
}
