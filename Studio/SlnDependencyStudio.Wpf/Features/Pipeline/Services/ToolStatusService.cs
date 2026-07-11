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
    private static readonly string[] KnownTools = ["d2", "mmdc"];

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

        // Seed with unknown entries so UI has something to bind to immediately.
        foreach (var tool in KnownTools)
        {
            _entries.Add(new ToolStatusEntry { ToolName = tool });
        }

        _statusSubject = new BehaviorSubject<IReadOnlyList<ToolStatusEntry>>(_entries.ToArray());
        ToolStatuses = _statusSubject.AsObservable();

        // Fire-and-forget initial scan.
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

        foreach (var entry in _entries)
        {
            var settings = _applicationSettings.CurrentSettings;
            _ = settings.ToolPathOverrides.TryGetValue(entry.ToolName, out var overridePath);

            var status = await detectionService
                .CheckToolAvailabilityAsync(entry.ToolName, overridePath, cancellationToken)
                .ConfigureAwait(true);

            entry.ToolName = status.ToolName;
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
