using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.Services;

/// <summary>
/// Detects and reports the availability of external CLI tools (d2, mmdc)
/// required by diagram renderers.
/// </summary>
public interface IToolStatusService : IStudioScopedDependency
{
    // The underlying <c>BehaviorSubject</c> replays the latest snapshot to late subscribers,
    // so the Pipeline page always receives the current state when it binds — no need to diff
    // individual collection changes.

    /// <summary>
    /// An observable that emits a complete snapshot of all tool statuses whenever
    /// a scan completes. Returns <c>IObservable&lt;IReadOnlyList&lt;T&gt;&gt;</c>
    /// rather than <c>ObservableCollection&lt;T&gt;</c> because tool detection is refreshed
    /// as a batch operation (<see cref="RescanAsync"/> produces a new full picture,
    /// not incremental adds/removes).
    /// </summary>
    IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }

    /// <summary>Re-scans for all required tools and updates <see cref="ToolStatuses"/>.</summary>
    Task RescanAsync(CancellationToken cancellationToken);
}
