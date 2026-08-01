using AllOverIt.Assertion;
using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SlnDependencyStudio.Cli.Handlers.Run;

/// <inheritdoc cref="ICommandLineRunHandler"/>
internal sealed class CommandLineRunHandler : CommandLineHandlerBase, ICommandLineRunHandler
{
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
        _generator = generator.WhenNotNull();
        _restoreSolutionRunner = restoreSolutionRunner.WhenNotNull();
        _preGenerationCommandRunner = preGenerationCommandRunner.WhenNotNull();
        _postGenerationCommandRunner = postGenerationCommandRunner.WhenNotNull();
        _projectValidator = projectValidator.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public override async Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken)
    {
        try
        {
            // Will throw DirectoryNotFoundException if the associated directory cannot be found
            var configDirectory = GetConfigDirectory(configFilename);

            var document = await LoadDependencyProjectDocumentAsync(configFilename, cancellationToken).ConfigureAwait(false);

            // Log the configuration to help with troubleshooting any validation errors.
            document.LogConfiguration(configFilename, _logger);

            // Validate all configuration up front so failures are reported before any command or
            // generation work begins. The command runners themselves do not perform validation.
            _projectValidator.Validate(document, configDirectory);

            if (!await RunRestoreSolutionIfRequiredAsync(document, cancellationToken))
            {
                return (int)StudioCliExitCode.DotNetRestoreFailed;
            }

            if (!await RunPreGenerationCommandIfRequiredAsync(document, cancellationToken))
            {
                return (int)StudioCliExitCode.PreGenerationCommandFailed;
            }

            await GenerateDiagramsAsync(document, cancellationToken);

            await RunPostGenerationCommandIfRequiredAsync(document, cancellationToken);

            _logger.LogInformation("Generation complete.");

            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return (int)StudioCliExitCode.RunCommandFailed;
        }
        catch (RegexParseException exception)
        {
            _logger.LogError("Invalid regular expression: {Message}", exception.Message);
            return (int)StudioCliExitCode.RunCommandFailed;
        }
        catch (ToolNotFoundException exception)
        {
            _logger.LogError("Required diagram tool not found: {Message}", exception.Message);
            return (int)StudioCliExitCode.DiagramToolNotFound;
        }
        catch (DependencyGeneratorException exception)
        {
            _logger.LogError("Diagram generator failed: {Message}", exception.Message);
            return (int)StudioCliExitCode.DiagramGeneratorFailed;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled.");
            return (int)StudioCliExitCode.RunCommandFailed;
        }
        catch (JsonException exception)
        {
            _logger.LogError("Failed to parse configuration file. Error on line {LineNumber} for Path {Path}.", exception.LineNumber + 1, exception.Path);
            return (int)StudioCliExitCode.CannotLoadConfigFile;
        }
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return (int)StudioCliExitCode.CannotLoadConfigFile;
        }
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
            _logger.LogInformation("Pre-generation command disabled.");
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
            _logger.LogError(
                "Pre-generation command failed ({ErrorCode}, exit code {ExitCode}, error: {ErrorMessage}).",
                preGenResult.ErrorCode,
                preGenResult.ExitCode,
                preGenResult.ErrorMessage);

            if (!preGenConfig.ContinueOnFailure)
            {
                _logger.LogError(
                    "Pre-generation command failed and continue-on-failure is disabled. Aborting.\n  {ErrorMessage}",
                    preGenResult.ErrorMessage);

                return false;
            }

            _logger.LogWarning(
                "Pre-generation command failed but continue-on-failure is enabled. Proceeding with generation.\n  {ErrorMessage}",
                preGenResult.ErrorMessage);
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
            _logger.LogInformation("Solution restore disabled.");
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

        if (restoreResult.Succeeded)
        {
            _logger.LogInformation("Solution restore completed successfully.");
            return true;
        }

        _logger.LogError(
            "Solution restore failed ({ErrorCode}, exit code {ExitCode}, error: {ErrorMessage}).",
            restoreResult.ErrorCode,
            restoreResult.ExitCode,
            restoreResult.ErrorMessage);

        return false;
    }

    /// <summary>Runs the post-generation command if enabled.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the post-generation command has finished.</returns>
    private async Task RunPostGenerationCommandIfRequiredAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        var postGenConfig = document.PostGeneration;

        if (!postGenConfig.Enabled)
        {
            _logger.LogInformation("Post-generation command disabled.");
            return;
        }

        _logger.LogInformation("Running Post-generation command...");

        // Subscribe to stdout/stderr so the output is captured in the CLI's log output.
        using var stdoutSub = _postGenerationCommandRunner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
        using var stderrSub = _postGenerationCommandRunner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

        var postGenResult = await _postGenerationCommandRunner.RunAsync(postGenConfig, cancellationToken);

        if (postGenResult.Succeeded)
        {
            _logger.LogInformation("Post-generation command completed successfully.");
            return;
        }

        _logger.LogWarning(
            "Post-generation command failed ({ErrorCode}, exit code {ExitCode}, error: {ErrorMessage}).",
            postGenResult.ErrorCode,
            postGenResult.ExitCode,
            postGenResult.ErrorMessage);
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
