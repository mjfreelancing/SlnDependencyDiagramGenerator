namespace SlnDependencyStudio.Shared.Logging;

/// <summary>
/// Accumulates <see cref="StudioLogEntry"/> instances emitted before the UI subscribes, then
/// transitions to live streaming.
/// </summary>
/// <remarks>
/// <para>Entries are queued (under a lock) until the first subscriber attaches. When that first
/// subscriber subscribes, the entire backlog is replayed to it in order before live entries are
/// streamed, so no pre-subscription entries are lost and no capacity limit needs to be guessed.
/// After the backlog is consumed the buffer acts purely as a live relay; the subscriber is
/// responsible for retaining any history it needs (for example, to re-filter on a later toggle).</para>
/// </remarks>
public interface IStudioLogBuffer : IObservable<StudioLogEntry>, IDisposable
{
    /// <summary>Adds a log entry. Queued until the first subscriber attaches, then streamed live.</summary>
    /// <param name="entry">The log entry to add.</param>
    void Add(StudioLogEntry entry);
}
