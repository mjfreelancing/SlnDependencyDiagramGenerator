using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs a dry-run analysis: discovers projects and checks tool readiness.
/// Results are streamed as <see cref="OutputMessage"/> events via the returned observable.
/// </summary>
internal sealed class PreGenerationAnalysisService : IPreGenerationAnalysisService
{
    private readonly IProjectDocumentStore _store;
    private readonly IScopedOperationFactory<IProjectDiscoveryService> _projectDiscoveryFactory;
    private readonly IToolStatusService _toolStatus;
    private readonly ILogger<PreGenerationAnalysisService> _logger;

    /// <summary>Initializes a new instance of <see cref="PreGenerationAnalysisService"/>.</summary>
    public PreGenerationAnalysisService(IProjectDocumentStore store, IScopedOperationFactory<IProjectDiscoveryService> projectDiscoveryFactory,
        IToolStatusService toolStatus, ILogger<PreGenerationAnalysisService> logger)
    {
        _store = store;
        _projectDiscoveryFactory = projectDiscoveryFactory;
        _toolStatus = toolStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    public IObservable<OutputMessage> RunAsync(CancellationToken cancellationToken)
    {
        return Observable.Create<OutputMessage>(async observer =>
        {
            try
            {
                _logger.LogDebug("Starting dry-run analysis");

                observer.OnNext(Info("=== Dry-Run Analysis ==="));

                var solutionPath = _store.SolutionOptionsEditor.SolutionPath.Value;

                if (solutionPath.IsNullOrEmpty())
                {
                    observer.OnNext(Error("No solution path configured"));
                    return;
                }

                // Resolve relative paths against the .sds project file's directory.
                if (_store.DocumentFilePath is not null)
                {
                    var projectDirectory = Path.GetDirectoryName(_store.DocumentFilePath)!;
                    solutionPath = PathUtils.ResolveAsAbsolutePath(solutionPath, projectDirectory);
                }

                var solutionEditor = _store.SolutionOptionsEditor;

                var includeRegex = solutionEditor.RegexToInclude.Items.Count > 0
                    ? solutionEditor.RegexToInclude.Items.ToArray()
                    : [".*\\.csproj"];

                var excludeRegex = solutionEditor.RegexToExclude.Items.ToArray();

                // Resolves a new, scoped, instance of IProjectDiscoveryService, calls DiscoverProjectsAsync(),
                // disposes of the scope and returns the result.
                var discoveryResult = await _projectDiscoveryFactory
                    .ExecuteAsync((discoveryService, token) =>
                    {
                        return discoveryService.DiscoverProjectsAsync(solutionPath, includeRegex, excludeRegex, token);
                    }, cancellationToken)
                    .ConfigureAwait(false);

                EmitProjectSection(observer, discoveryResult);

                observer.OnNext(Info("=== Tool Readiness ==="));

                var toolEntries = await _toolStatus.ToolStatuses.FirstAsync();

                foreach (var entry in toolEntries)
                {
                    var icon = entry.IsAvailable ? "\u2713" : "\u2717";
                    var message = $"{icon} {entry.ToolName}: {entry.StatusText}";

                    observer.OnNext(entry.IsAvailable
                        ? Info(message)
                        : Error(message));
                }

                observer.OnNext(Info("Analysis complete"));
            }
            catch (OperationCanceledException)
            {
                observer.OnNext(Warning("Analysis cancelled"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis failed");
                observer.OnNext(Error($"Analysis failed: {ex.Message}"));
            }
            finally
            {
                observer.OnCompleted();
            }
        });
    }

    private static void EmitProjectSection(IObserver<OutputMessage> observer, ProjectDiscoveryResult discoveryResult)
    {
        observer.OnNext(Info($"Projects Discovered: {discoveryResult.AllProjectPaths.Length}"));

        foreach (var path in discoveryResult.AllProjectPaths)
        {
            observer.OnNext(Info($"  {Path.GetFileName(path)}  [{path}]"));
        }

        observer.OnNext(Info($"Included: {discoveryResult.IncludedProjectPaths.Length}"));

        foreach (var path in discoveryResult.IncludedProjectPaths)
        {
            observer.OnNext(Info($"  {Path.GetFileName(path)}  [{path}]"));
        }

        if (discoveryResult.ExcludedProjectPaths.Length > 0)
        {
            observer.OnNext(Warning($"Excluded (regex): {discoveryResult.ExcludedProjectPaths.Length}"));

            foreach (var path in discoveryResult.ExcludedProjectPaths)
            {
                observer.OnNext(Warning($"  {Path.GetFileName(path)}  [{path}]"));
            }
        }
        else
        {
            observer.OnNext(Info("Excluded (regex): 0"));
        }

        if (discoveryResult.ImplicitlyExcludedProjectPaths.Length > 0)
        {
            observer.OnNext(Warning($"Not Matched: {discoveryResult.ImplicitlyExcludedProjectPaths.Length}"));

            foreach (var path in discoveryResult.ImplicitlyExcludedProjectPaths)
            {
                observer.OnNext(Warning($"  {Path.GetFileName(path)}  [{path}]"));
            }
        }
        else
        {
            observer.OnNext(Info("Not Matched: 0"));
        }
    }

    private static OutputMessage Info(string text) => new() { Text = text, Level = OutputMessageLevel.Information };
    private static OutputMessage Warning(string text) => new() { Text = text, Level = OutputMessageLevel.Warning };
    private static OutputMessage Error(string text) => new() { Text = text, Level = OutputMessageLevel.Error };
}
