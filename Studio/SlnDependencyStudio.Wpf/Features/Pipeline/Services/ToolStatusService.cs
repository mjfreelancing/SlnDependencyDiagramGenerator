using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IApplicationSettingsService _applicationSettings;

    private readonly ObservableCollection<ToolStatusEntry> _entries = [];
    private readonly BehaviorSubject<IReadOnlyList<ToolStatusEntry>> _statusSubject;

    /// <summary>Initializes a new instance of <see cref="ToolStatusService"/>.</summary>
    /// <param name="scopeFactory">Factory for creating scopes to resolve scoped dependencies.</param>
    /// <param name="applicationSettings">Provides access to tool path overrides.</param>
    public ToolStatusService(
        IServiceScopeFactory scopeFactory,
        IApplicationSettingsService applicationSettings)
    {
        _scopeFactory = scopeFactory;
        _applicationSettings = applicationSettings;

        _statusSubject = new BehaviorSubject<IReadOnlyList<ToolStatusEntry>>(_entries.ToArray());
        ToolStatuses = _statusSubject.AsObservable();

        // Fire-and-forget initial scan — this will seed entries from the detection service.
        _ = RescanAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }

    /// <inheritdoc />
    public async Task RescanAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var detectionService = scope.ServiceProvider.GetRequiredService<IToolDetectionService>();

        var now = DateTime.UtcNow;

        // Seed or sync entries from the detection service's known tool list.
        var knownToolNames = detectionService.KnownToolNames;

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (!knownToolNames.Contains(_entries[i].ToolName))
            {
                _entries.RemoveAt(i);
            }
        }

        foreach (var toolName in knownToolNames)
        {
            if (!_entries.Any(e => e.ToolName == toolName))
            {
                _entries.Add(new ToolStatusEntry { ToolName = toolName });
            }
        }

        foreach (var entry in _entries)
        {
            var settings = _applicationSettings.CurrentSettings;
            _ = settings.ToolPathOverrides.TryGetValue(entry.ToolName, out var overridePath);

            var status = await detectionService
                .CheckToolAvailabilityAsync(entry.ToolName, overridePath, cancellationToken)
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
