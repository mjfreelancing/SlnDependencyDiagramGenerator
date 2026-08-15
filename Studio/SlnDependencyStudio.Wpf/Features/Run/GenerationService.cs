using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.ProcessExecution;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Services;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs the full generation pipeline: restore solution (if enabled), pre-generation command (if enabled),
/// diagram generation, and post-generation command (if enabled).
/// </summary>
internal sealed class GenerationService : IGenerationService
{
    private readonly IProjectDocumentStore _store;
    private readonly IDependencyProjectValidator _projectValidator;
    private readonly IScopedOperationFactory<IRestoreSolutionRunner> _restoreRunnerFactory;
    private readonly IScopedOperationFactory<IPreGenerationCommandRunner> _runnerFactory;
    private readonly IScopedOperationFactory<IPostGenerationCommandRunner> _postGenRunnerFactory;
    private readonly IScopedOperationFactory<IDependencyGenerator> _generatorFactory;
    private readonly ILogger<GenerationService> _logger;

    /// <summary>Initializes a new instance of <see cref="GenerationService"/>.</summary>
    public GenerationService(IProjectDocumentStore store, IDependencyProjectValidator projectValidator,
        IScopedOperationFactory<IRestoreSolutionRunner> restoreRunnerFactory,
        IScopedOperationFactory<IPreGenerationCommandRunner> runnerFactory,
        IScopedOperationFactory<IPostGenerationCommandRunner> postGenRunnerFactory,
        IScopedOperationFactory<IDependencyGenerator> generatorFactory, ILogger<GenerationService> logger)
    {
        _store = store;
        _projectValidator = projectValidator;
        _restoreRunnerFactory = restoreRunnerFactory;
        _runnerFactory = runnerFactory;
        _postGenRunnerFactory = postGenRunnerFactory;
        _generatorFactory = generatorFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting generation");

            // Validate all configuration up front so the pipeline fails fast before any command or
            // generation work begins. The command runners themselves do not perform validation.
            var document = _store.BuildDocument();

            _projectValidator.Validate(document, _store.DocumentDirectory);

            document.LogConfiguration(_store.DocumentFilePath!, _logger);

            var config = document.DiagramGenerator;

            var shouldContinue = await RunRestoreSolutionAsync(config.Solution.SolutionPath, cancellationToken).ConfigureAwait(false);

            if (shouldContinue)
            {
                shouldContinue = await RunPreGenerationAsync(cancellationToken).ConfigureAwait(false);
            }

            if (shouldContinue)
            {
                await RunDiagramGenerationAsync(config, cancellationToken).ConfigureAwait(false);

                await RunPostGenerationAsync(cancellationToken).ConfigureAwait(false);
            }

            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogInformation("Generation completed ({Elapsed:F1}s)", elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Generation cancelled");
        }
        catch (ValidationException exception)
        {
            _logger.LogError("Configuration validation failed:");

            foreach (var error in exception.Errors)
            {
                _logger.LogError("  - {ErrorMessage}", error.ErrorMessage);
            }
        }
        catch (DependencyGeneratorException exception)
        {
            // Well-known generator failures (e.g. missing tools) are self-describing via
            // type + message; the stack trace adds noise for these expected outcomes.
            _logger.LogError("Generation failed: {Message}", exception.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generation failed");
        }
    }

    /// <summary>Runs the diagram generator.</summary>
    /// <param name="config">The generator configuration built from the store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task RunDiagramGenerationAsync(DependencyGeneratorConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating diagrams...");

        await _generatorFactory
            .ExecuteAsync((generator, token) => generator.CreateDiagramsAsync(config, token), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Runs the pre-generation command if enabled in the store.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the pipeline should continue; <see langword="false"/> when it should stop.</returns>
    internal async Task<bool> RunPreGenerationAsync(CancellationToken cancellationToken)
    {
        var preGen = _store.PreGenerationEditor;

        if (!preGen.Enabled.Value || preGen.Command.Value.IsNullOrEmpty())
        {
            _logger.LogDebug("Pre-generation command disabled or has no command — skipping");

            return true;
        }

        _logger.LogInformation("Running pre-generation command...");

        var workingDirectory = preGen.WorkingDirectory.Value;

        if (workingDirectory.IsNotNullOrEmpty() && _store.DocumentFilePath is not null)
        {
            var projectDir = Path.GetDirectoryName(_store.DocumentFilePath)!;
            workingDirectory = PathUtils.ResolveAsAbsolutePath(workingDirectory, projectDir);
        }

        var preGenConfig = new PreGenerationConfig
        {
            Enabled = preGen.Enabled.Value,
            Command = preGen.Command.Value,
            Arguments = preGen.Arguments.Value,
            WorkingDirectory = workingDirectory,
            ContinueOnFailure = preGen.ContinueOnFailure.Value
        };

        var preGenResult = await _runnerFactory
            .ExecuteAsync(async (runner, token) =>
            {
                using var stdoutSub = runner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
                using var stderrSub = runner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

                return await runner.RunAsync(preGenConfig, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (preGenResult.Succeeded)
        {
            _logger.LogInformation("Pre-generation command completed successfully");

            return true;
        }

        if (preGenResult.ErrorCode == CommandErrorCode.Cancelled)
        {
            if (preGenConfig.Arguments.IsNullOrEmpty())
            {
                _logger.LogWarning("Pre-generation command '{Command}' (with no args) was cancelled", preGenConfig.Command);
            }
            else
            {
                _logger.LogWarning("Pre-generation command '{Command}' with args '{Arguments}' was cancelled",
                    preGenConfig.Command, preGenConfig.Arguments);
            }

            return false;
        }

        if (preGenConfig.ContinueOnFailure)
        {
            _logger.LogWarning("Pre-generation command failed (continuing): {Message}", preGenResult.ErrorMessage);

            return true;
        }

        _logger.LogError("Pre-generation command failed: {Message}", preGenResult.ErrorMessage);

        return false;
    }

    /// <summary>Restores the solution (via <c>dotnet restore</c>) if enabled in the store.</summary>
    /// <param name="solutionPath">The fully-qualified path to the solution to restore.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the pipeline should continue; <see langword="false"/> when it should stop.</returns>
    internal async Task<bool> RunRestoreSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        if (!_store.RestoreSolutionEditor.RestoreSolution.Value)
        {
            _logger.LogDebug("Solution restore disabled — skipping");

            return true;
        }

        if (solutionPath.IsNullOrEmpty())
        {
            _logger.LogError("Solution restore failed: no solution path is configured.");

            return false;
        }

        _logger.LogInformation("Restoring solution: {SolutionPath}", solutionPath);

        var restoreResult = await _restoreRunnerFactory
            .ExecuteAsync(async (runner, token) =>
            {
                using var stdoutSub = runner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
                using var stderrSub = runner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

                return await runner.RunAsync(solutionPath, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (restoreResult.Succeeded)
        {
            _logger.LogInformation("Solution restore completed successfully");

            return true;
        }

        _logger.LogError("Solution restore failed: {Message}", restoreResult.ErrorMessage);

        return false;
    }

    /// <summary>Runs the post-generation command if enabled in the store.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task RunPostGenerationAsync(CancellationToken cancellationToken)
    {
        var postGen = _store.PostGenerationEditor;

        if (!postGen.Enabled.Value || postGen.Command.Value.IsNullOrEmpty())
        {
            _logger.LogDebug("Post-generation command disabled or has no command — skipping");

            return;
        }

        _logger.LogInformation("Running post-generation command...");

        var workingDirectory = postGen.WorkingDirectory.Value;

        if (workingDirectory.IsNotNullOrEmpty() && _store.DocumentFilePath is not null)
        {
            var projectDir = Path.GetDirectoryName(_store.DocumentFilePath)!;
            workingDirectory = PathUtils.ResolveAsAbsolutePath(workingDirectory, projectDir);
        }

        var postGenConfig = new PostGenerationConfig
        {
            Enabled = postGen.Enabled.Value,
            Command = postGen.Command.Value,
            Arguments = postGen.Arguments.Value,
            WorkingDirectory = workingDirectory
        };

        var postGenResult = await _postGenRunnerFactory
            .ExecuteAsync(async (runner, token) =>
            {
                using var stdoutSub = runner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
                using var stderrSub = runner.StdErr.Subscribe(line => _logger.LogWarning("{Line}", line));

                return await runner.RunAsync(postGenConfig, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (postGenResult.Succeeded)
        {
            _logger.LogInformation("Post-generation command completed successfully");

            return;
        }

        _logger.LogWarning("Post-generation command failed: {Message}", postGenResult.ErrorMessage);
    }
}
