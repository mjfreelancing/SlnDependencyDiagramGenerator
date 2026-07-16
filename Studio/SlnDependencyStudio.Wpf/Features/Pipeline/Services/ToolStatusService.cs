using SlnDependencyDiagramGenerator.Generator.ToolDetection;
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
    private readonly IToolPathResolver _toolPathResolver;
    private readonly IToolDetectionService _toolDetection;

    private readonly ObservableCollection<ToolStatusEntry> _entries = [];
    private readonly BehaviorSubject<IReadOnlyList<ToolStatusEntry>> _statusSubject;

    /// <inheritdoc />
    public IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }

    /// <summary>Initializes a new instance of <see cref="ToolStatusService"/>.</summary>
    /// <param name="toolPathResolver">Resolves effective tool paths for external CLI tools.</param>
    /// <param name="toolDetection">The tool detection service.</param>
    public ToolStatusService(IToolPathResolver toolPathResolver, IToolDetectionService toolDetection)
    {
        _toolPathResolver = toolPathResolver;
        _toolDetection = toolDetection;

        _statusSubject = new BehaviorSubject<IReadOnlyList<ToolStatusEntry>>([.. _entries]);
        ToolStatuses = _statusSubject.AsObservable();
    }

    /// <inheritdoc />
    public async Task RescanAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Seed or sync entries from the detection service's known tool list.
        var knownToolNames = _toolDetection.KnownToolNames;

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
            var status = await _toolDetection
                .CheckToolAvailabilityAsync(entry.ToolName, cancellationToken)
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
