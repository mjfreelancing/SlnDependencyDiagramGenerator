using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.Services;

/// <summary>
/// Default implementation of <see cref="IToolStatusService"/>. Detects d2 and mmdc
/// availability by delegating to <see cref="IToolDetectionService"/>.
/// </summary>
internal sealed class ToolStatusService : IToolStatusService, IDisposable
{
    private readonly IScopedOperationFactory<IToolDetectionService> _toolDetectionFactory;
    private readonly IApplicationSettingsService _applicationSettings;

    private readonly ObservableCollection<ToolStatusEntry> _entries = [];
    private readonly BehaviorSubject<IReadOnlyList<ToolStatusEntry>> _statusSubject;

    /// <inheritdoc />
    public IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }

    /// <summary>Initializes a new instance of <see cref="ToolStatusService"/>.</summary>
    /// <param name="toolDetectionFactory">Factory for creating scoped <see cref="IToolDetectionService"/> operations.</param>
    /// <param name="applicationSettings">Provides access to tool path overrides.</param>
    public ToolStatusService(IScopedOperationFactory<IToolDetectionService> toolDetectionFactory, IApplicationSettingsService applicationSettings)
    {
        _toolDetectionFactory = toolDetectionFactory;
        _applicationSettings = applicationSettings;

        _statusSubject = new BehaviorSubject<IReadOnlyList<ToolStatusEntry>>([.. _entries]);
        ToolStatuses = _statusSubject.AsObservable();
    }

    /// <inheritdoc />
    public async Task RescanAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Seed or sync entries from the detection service's known tool list.
        var knownToolNames = _toolDetectionFactory.Execute(svc => svc.KnownToolNames);

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (!knownToolNames.Contains(_entries[i].ToolName))
            {
                _entries.RemoveAt(i);
            }
        }

        foreach (var toolName in knownToolNames)
        {
            if (!_entries.Any(entry => entry.ToolName == toolName))
            {
                _entries.Add(new ToolStatusEntry { ToolName = toolName });
            }
        }

        foreach (var entry in _entries)
        {
            var settings = _applicationSettings.CurrentSettings;
            _ = settings.ToolPathOverrides.TryGetValue(entry.ToolName, out var overridePath);

            // Resolves a new, scoped, instance of IToolDetectionService, calls DiscoverProjectsAsync(),
            // disposes of the scope and returns the result.
            var status = await _toolDetectionFactory
                .ExecuteAsync((detectionService, token) =>
                {
                    return detectionService.CheckToolAvailabilityAsync(entry.ToolName, overridePath, token);
                }, cancellationToken)
                .ConfigureAwait(true);

            entry.IsAvailable = status.IsAvailable;
            entry.ResolvedPath = status.ResolvedPath;
            entry.ErrorMessage = status.ErrorMessage;
            entry.LastChecked = now;
        }

        _statusSubject.OnNext([.. _entries]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _statusSubject.Dispose();
    }
}
