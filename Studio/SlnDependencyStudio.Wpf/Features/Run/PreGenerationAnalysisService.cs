using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyStudio.Shared.Services;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs a dry-run analysis: discovers projects and checks tool readiness.
/// </summary>
internal sealed class PreGenerationAnalysisService : IPreGenerationAnalysisService
{
    private readonly IProjectDocumentStore _store;
    private readonly IDependencyProjectValidator _projectValidator;
    private readonly IScopedOperationFactory<IProjectDiscoveryService> _projectDiscoveryFactory;
    private readonly IToolStatusService _toolStatus;
    private readonly ILogger<PreGenerationAnalysisService> _logger;

    /// <summary>Initializes a new instance of <see cref="PreGenerationAnalysisService"/>.</summary>
    public PreGenerationAnalysisService(IProjectDocumentStore store, IDependencyProjectValidator projectValidator,
        IScopedOperationFactory<IProjectDiscoveryService> projectDiscoveryFactory,
        IToolStatusService toolStatus, ILogger<PreGenerationAnalysisService> logger)
    {
        _store = store;
        _projectValidator = projectValidator;
        _projectDiscoveryFactory = projectDiscoveryFactory;
        _toolStatus = toolStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting dry-run analysis");

            // Validate all configuration up front so issues are detected early during the dry-run.
            // The command runners themselves do not perform validation.
            _projectValidator.Validate(_store.BuildDocument(), _store.DocumentDirectory);

            var solutionPath = _store.SolutionOptionsEditor.SolutionPath.Value;

            if (solutionPath.IsNullOrEmpty())
            {
                _logger.LogError("No solution path configured");
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

            EmitProjectSection(discoveryResult);

            _logger.LogInformation("Checking tool readiness");

            // ToolStatuses replays the current snapshot on subscription, so FirstAsync() can take
            // that snapshot once for the readiness report and complete, rather than subscribing
            // to the ongoing stream.
            var toolEntries = await _toolStatus.ToolStatuses.FirstAsync();

            foreach (var entry in toolEntries)
            {
                var icon = entry.IsAvailable ? "\u2713" : "\u2717";

                if (entry.IsAvailable)
                {
                    _logger.LogInformation("{Icon} {ToolName}: {StatusText}", icon, entry.ToolName, entry.StatusText);
                }
                else
                {
                    _logger.LogError("{Icon} {ToolName}: {StatusText}", icon, entry.ToolName, entry.StatusText);
                }
            }

            _logger.LogInformation("Analysis complete");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Analysis cancelled");
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
            // Well-known generator failures (e.g. solution parse problems) are self-describing
            // via type + message; the stack trace adds noise for these expected outcomes.
            _logger.LogError("Analysis failed: {Message}", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Analysis failed unexpectedly");
        }
    }

    private void EmitProjectSection(ProjectDiscoveryResult discoveryResult)
    {
        _logger.LogInformation("Projects Discovered: {Count}", discoveryResult.AllProjectPaths.Length);

        foreach (var path in discoveryResult.AllProjectPaths)
        {
            _logger.LogInformation("  {FileName}  [{Path}]", Path.GetFileName(path), path);
        }

        _logger.LogInformation("Included: {Count}", discoveryResult.IncludedProjectPaths.Length);

        foreach (var path in discoveryResult.IncludedProjectPaths)
        {
            _logger.LogInformation("  {FileName}  [{Path}]", Path.GetFileName(path), path);
        }

        if (discoveryResult.ExcludedProjectPaths.Length > 0)
        {
            _logger.LogInformation("Excluded (regex): {Count}", discoveryResult.ExcludedProjectPaths.Length);

            foreach (var path in discoveryResult.ExcludedProjectPaths)
            {
                _logger.LogInformation("  {FileName}  [{Path}]", Path.GetFileName(path), path);
            }
        }
        else
        {
            _logger.LogInformation("Excluded (regex): 0");
        }

        if (discoveryResult.ImplicitlyExcludedProjectPaths.Length > 0)
        {
            _logger.LogInformation("Not Matched: {Count}", discoveryResult.ImplicitlyExcludedProjectPaths.Length);

            foreach (var path in discoveryResult.ImplicitlyExcludedProjectPaths)
            {
                _logger.LogInformation("  {FileName}  [{Path}]", Path.GetFileName(path), path);
            }
        }
        else
        {
            _logger.LogInformation("Not Matched: 0");
        }
    }
}
