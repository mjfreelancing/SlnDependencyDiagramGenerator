using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.ProcessExecution;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Services;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs the full generation pipeline: restore solution (if enabled), pre-generation command (if enabled),
/// diagram generation, and post-generation command (if enabled).
/// Results are streamed as <see cref="OutputMessage"/> events.
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
    public IObservable<OutputMessage> RunAsync(CancellationToken cancellationToken)
    {
        return Observable.Create<OutputMessage>(async observer =>
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Generation started");

                observer.OnNext(Info("=== Generation Started ==="));

                // Validate all configuration up front so the pipeline fails fast before any command or
                // generation work begins. The command runners themselves do not perform validation.
                _projectValidator.Validate(_store.BuildDocument(), _store.DocumentDirectory);

                var config = _store.BuildGeneratorConfig();

                var shouldContinue = await RunRestoreSolutionAsync(observer, config.Solution.SolutionPath, cancellationToken).ConfigureAwait(false);

                if (shouldContinue)
                {
                    shouldContinue = await RunPreGenerationAsync(observer, cancellationToken).ConfigureAwait(false);
                }

                if (shouldContinue)
                {
                    await RunDiagramGenerationAsync(observer, config, cancellationToken).ConfigureAwait(false);

                    await RunPostGenerationAsync(observer, cancellationToken).ConfigureAwait(false);
                }

                var elapsed = DateTime.UtcNow - startTime;

                _logger.LogInformation("Generation completed in {Elapsed:F1}s", elapsed.TotalSeconds);

                observer.OnNext(Info($"=== Generation Completed ({elapsed.TotalSeconds:F1}s) ==="));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Generation cancelled");

                observer.OnNext(Warning("Generation cancelled"));
            }
            catch (ValidationException exception)
            {
                _logger.LogWarning("Configuration validation failed");

                observer.OnNext(Error($"Configuration validation failed: {exception.Message}"));
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Generation timed out");

                observer.OnNext(Error($"Generation timed out: {ex.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generation failed");

                observer.OnNext(Error($"Generation failed: {ex.Message}"));
            }
            finally
            {
                observer.OnCompleted();
            }
        });
    }

    /// <summary>
    /// Runs the diagram generator, streaming progress to the observer.
    /// </summary>
    internal async Task RunDiagramGenerationAsync(IObserver<OutputMessage> observer, DependencyGeneratorConfig config,
        CancellationToken cancellationToken)
    {
        observer.OnNext(Info("Generating diagrams…"));

        await _generatorFactory
            .ExecuteAsync((generator, token) => generator.CreateDiagramsAsync(config, token), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the pre-generation command if enabled in the store.
    /// </summary>
    /// <returns><see langword="true"/> when the pipeline should continue; <see langword="false"/> when it should stop.</returns>
    internal async Task<bool> RunPreGenerationAsync(IObserver<OutputMessage> observer, CancellationToken cancellationToken)
    {
        var preGen = _store.PreGenerationEditor;

        if (!preGen.Enabled.Value || preGen.Command.Value.IsNullOrEmpty())
        {
            return true;
        }

        observer.OnNext(Info("Running pre-generation command…"));

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
                using var stdoutSub = runner.StdOut.Subscribe(line => observer.OnNext(Info(line)));
                using var stderrSub = runner.StdErr.Subscribe(line => observer.OnNext(Warning(line)));

                return await runner.RunAsync(preGenConfig, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (preGenResult.Succeeded)
        {
            observer.OnNext(Info("Pre-generation command completed successfully"));

            return true;
        }

        if (preGenResult.ErrorCode == CommandErrorCode.Cancelled)
        {
            var sb = new StringBuilder();

            sb.Append($"Pre-generation command '{preGenConfig.Command}' ");

            if (preGenConfig.Arguments.IsNullOrEmpty())
            {
                sb.Append("(with no args) ");
            }
            else
            {
                sb.Append($"with args '{preGenConfig.Arguments}' ");
            }

            sb.Append("was cancelled");

            observer.OnNext(Warning(sb.ToString()));

            return false;
        }

        if (preGenConfig.ContinueOnFailure)
        {
            observer.OnNext(Warning($"Pre-generation command failed (continuing): {preGenResult.ErrorMessage}"));

            return true;
        }

        observer.OnNext(Error($"Pre-generation command failed: {preGenResult.ErrorMessage}"));

        return false;
    }

    /// <summary>
    /// Restores the solution (via <c>dotnet restore</c>) if enabled in the store.
    /// </summary>
    /// <param name="observer">The output observer.</param>
    /// <param name="solutionPath">The fully-qualified path to the solution to restore.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the pipeline should continue; <see langword="false"/> when it should stop.</returns>
    internal async Task<bool> RunRestoreSolutionAsync(IObserver<OutputMessage> observer, string solutionPath,
        CancellationToken cancellationToken)
    {
        if (!_store.RestoreSolutionEditor.RestoreSolution.Value)
        {
            return true;
        }

        if (solutionPath.IsNullOrEmpty())
        {
            observer.OnNext(Error("Solution restore failed: no solution path is configured."));

            return false;
        }

        observer.OnNext(Info("Restoring solution…"));

        var restoreResult = await _restoreRunnerFactory
            .ExecuteAsync(async (runner, token) =>
            {
                using var stdoutSub = runner.StdOut.Subscribe(line => observer.OnNext(Info(line)));
                using var stderrSub = runner.StdErr.Subscribe(line => observer.OnNext(Warning(line)));

                return await runner.RunAsync(solutionPath, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (restoreResult.Succeeded)
        {
            observer.OnNext(Info("Solution restore completed successfully"));

            return true;
        }

        observer.OnNext(Error($"Solution restore failed: {restoreResult.ErrorMessage}"));

        return false;
    }

    /// <summary>
    /// Runs the post-generation command if enabled in the store.
    /// </summary>
    internal async Task RunPostGenerationAsync(IObserver<OutputMessage> observer, CancellationToken cancellationToken)
    {
        var postGen = _store.PostGenerationEditor;

        if (!postGen.Enabled.Value || postGen.Command.Value.IsNullOrEmpty())
        {
            return;
        }

        observer.OnNext(Info("Running post-generation command…"));

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
                using var stdoutSub = runner.StdOut.Subscribe(line => observer.OnNext(Info(line)));
                using var stderrSub = runner.StdErr.Subscribe(line => observer.OnNext(Warning(line)));

                return await runner.RunAsync(postGenConfig, token);
            }, cancellationToken)
            .ConfigureAwait(false);

        if (postGenResult.Succeeded)
        {
            observer.OnNext(Info("Post-generation command completed successfully"));

            return;
        }

        observer.OnNext(Warning($"Post-generation command failed: {postGenResult.ErrorMessage}"));
    }

    private static OutputMessage Info(string text) => new() { Text = text, Level = OutputMessageLevel.Information };
    private static OutputMessage Warning(string text) => new() { Text = text, Level = OutputMessageLevel.Warning };
    private static OutputMessage Error(string text) => new() { Text = text, Level = OutputMessageLevel.Error };
}
