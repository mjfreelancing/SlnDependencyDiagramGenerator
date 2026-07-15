using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;
using SlnDependencyStudio.Shared.PreGeneration;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs the full generation pipeline: pre-generation command + diagram generation.
/// Results are streamed as <see cref="OutputMessage"/> events.
/// </summary>
internal sealed class GenerationService : IGenerationService
{
    private readonly IProjectDocumentStore _store;
    private readonly IScopedOperationFactory<IPreGenerationCommandRunner> _runnerFactory;
    private readonly IScopedOperationFactory<IDependencyGenerator> _generatorFactory;
    private readonly ILogger<GenerationService> _logger;

    /// <summary>Initializes a new instance of <see cref="GenerationService"/>.</summary>
    public GenerationService(IProjectDocumentStore store, IScopedOperationFactory<IPreGenerationCommandRunner> runnerFactory,
        IScopedOperationFactory<IDependencyGenerator> generatorFactory, ILogger<GenerationService> logger)
    {
        _store = store;
        _runnerFactory = runnerFactory;
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

                var config = _store.BuildGeneratorConfig();

                var shouldContinue = await RunPreGenerationAsync(observer, cancellationToken).ConfigureAwait(false);

                if (shouldContinue)
                {
                    await RunDiagramGenerationAsync(observer, config, cancellationToken).ConfigureAwait(false);
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

        if (preGenResult.ExitCode == StudioExitCode.PreGenerationCommandCancelled.Value)
        {
            observer.OnNext(Warning("Pre-generation command was cancelled"));

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
    /// Runs the diagram generator, streaming progress to the observer.
    /// </summary>
    internal async Task RunDiagramGenerationAsync(IObserver<OutputMessage> observer, DependencyGeneratorConfig config,
        CancellationToken cancellationToken)
    {
        observer.OnNext(Info("Generating diagrams…"));

        await _generatorFactory
            .ExecuteAsync(async (generator, token) =>
            {
                using var progressSub = generator.OnProgress.Subscribe(line => observer.OnNext(Info(line)));

                await generator.CreateDiagramsAsync(config, token);
            }, cancellationToken)
            .ConfigureAwait(false);
    }

    private static OutputMessage Info(string text) => new() { Text = text, Level = OutputMessageLevel.Information };
    private static OutputMessage Warning(string text) => new() { Text = text, Level = OutputMessageLevel.Warning };
    private static OutputMessage Error(string text) => new() { Text = text, Level = OutputMessageLevel.Error };
}
