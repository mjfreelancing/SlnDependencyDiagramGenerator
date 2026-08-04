using System.Reactive.Subjects;

namespace SlnDependencyStudio.Shared.Logging;

// ==========================================================================================
// Purpose of this custom logging implementation
// ==========================================================================================
//
// Problem
// ------------------------------------------------------------------------------------------
// Applications log from the moment they start. Some log destinations are available
// immediately (for example, a rolling file on disk), but others are only ready later — such as
// a user-facing log panel that cannot subscribe until the UI has been constructed and shown.
//
// A push-based logging pipeline only delivers each event to the consumers subscribed at the
// moment the event is emitted. Any event logged before the UI is ready is therefore dropped
// from the UI, even though those early events are often the most useful for diagnosing
// startup problems. The volume of startup logging is not known in advance, so pre-allocating a
// fixed-size capture buffer is awkward and easy to get wrong.
//
// Solution
// ------------------------------------------------------------------------------------------
// Capture every event from the moment the logging pipeline starts, without guessing how many
// there will be. Retain the captured events only until the UI is ready. When the UI subscribes,
// deliver everything captured so far in order, then switch to live streaming and retain nothing
// further. The UI is then free to filter what it displays, applying the same filter to both the
// captured snapshot and subsequent live events.
//
// Concurrency
// ------------------------------------------------------------------------------------------
// Events can be produced from any thread, so all transitions are guarded by a single lock. The
// switch from "capturing" to "streaming live" happens atomically within the first subscription,
// so no event is lost in the gap and none is delivered twice.

/// <summary>
/// Default implementation of <see cref="IStudioLogBuffer"/>.
/// </summary>
/// <remarks>
/// <para>Queue-based pre-subscription capture: entries are queued under a lock until the first
/// subscriber attaches. On that first subscription the entire backlog is replayed in order and the
/// buffer switches to a live <see cref="Subject{T}"/> relay. No capacity limit is required because
/// the backlog is bounded in practice by the time between application start and the first
/// subscriber (for example, the WPF output panel subscribing during main-window construction).</para>
/// </remarks>
public sealed class StudioLogBuffer : IStudioLogBuffer
{
    private readonly Lock _syncRoot = new();
    private readonly Queue<StudioLogEntry> _backlog = [];
    private readonly Subject<StudioLogEntry> _subject = new();
    private bool _streaming;
    private bool _disposed;

    /// <inheritdoc />
    public void Add(StudioLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_streaming)
            {
                _subject.OnNext(entry);
            }
            else
            {
                _backlog.Enqueue(entry);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<StudioLogEntry> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Attach the live subscription and switch to streaming before replaying the backlog.
            // Holding the lock for the whole transition makes the queue-to-live switch atomic from
            // the producer's point of view: no entry is dropped and none is delivered twice.
            // The subscriber's OnNext only schedules work (for example, via ObserveOn), so calling
            // it while holding the lock does not re-enter Add.
            var subscription = _subject.Subscribe(observer);

            _streaming = true;

            while (_backlog.Count > 0)
            {
                observer.OnNext(_backlog.Dequeue());
            }

            return subscription;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _subject.OnCompleted();
            _subject.Dispose();

            _disposed = true;
        }
    }
}
