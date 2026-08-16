using NSubstitute;
using Serilog.Events;
using Shouldly;
using SlnDependencyStudio.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Shared.Tests.Unit.Logging;

public class StudioLogBufferFixture
{
    [Fact]
    public void Should_Replay_Backlog_On_First_Subscription()
    {
        var buffer = new StudioLogBuffer();
        buffer.Add(CreateEntry(LogEventLevel.Information, "one"));
        buffer.Add(CreateEntry(LogEventLevel.Information, "two"));

        var received = new List<StudioLogEntry>();
        using var subscription = buffer.Subscribe(received.Add);

        received.Select(entry => entry.Message).ShouldBe(new[] { "one", "two" });
    }

    [Fact]
    public void Should_Stream_Live_Events_After_Subscription()
    {
        var buffer = new StudioLogBuffer();

        var received = new List<StudioLogEntry>();
        using var subscription = buffer.Subscribe(received.Add);

        buffer.Add(CreateEntry(LogEventLevel.Information, "live"));

        received.Single().Message.ShouldBe("live");
    }

    [Fact]
    public void Should_Preserve_Order_Between_Backlog_And_Live()
    {
        var buffer = new StudioLogBuffer();
        buffer.Add(CreateEntry(LogEventLevel.Information, "backlog"));

        var received = new List<StudioLogEntry>();
        using var subscription = buffer.Subscribe(received.Add);

        buffer.Add(CreateEntry(LogEventLevel.Information, "live"));

        received.Select(entry => entry.Message).ShouldBe(new[] { "backlog", "live" });
    }

    [Fact]
    public void Should_Not_Duplicate_Entries()
    {
        var buffer = new StudioLogBuffer();
        buffer.Add(CreateEntry(LogEventLevel.Information, "one"));

        var received = new List<StudioLogEntry>();
        using var subscription = buffer.Subscribe(received.Add);

        buffer.Add(CreateEntry(LogEventLevel.Information, "two"));

        received.Select(entry => entry.Message).ShouldBe(new[] { "one", "two" });
    }

    [Fact]
    public void Should_Not_Receive_Events_After_Unsubscribe()
    {
        var buffer = new StudioLogBuffer();

        var received = new List<StudioLogEntry>();
        var subscription = buffer.Subscribe(received.Add);

        subscription.Dispose();

        buffer.Add(CreateEntry(LogEventLevel.Information, "dropped"));

        received.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Throw_When_Adding_Null()
    {
        var buffer = new StudioLogBuffer();

        Should.Throw<ArgumentNullException>(() => buffer.Add(null!));
    }

    [Fact]
    public void Should_Throw_When_Subscribing_Null()
    {
        var buffer = new StudioLogBuffer();

        Should.Throw<ArgumentNullException>(() => buffer.Subscribe(null!));
    }

    [Fact]
    public void Should_Throw_When_Adding_After_Dispose()
    {
        var buffer = new StudioLogBuffer();
        buffer.Dispose();

        Should.Throw<ObjectDisposedException>(() => buffer.Add(CreateEntry(LogEventLevel.Information, "late")));
    }

    [Fact]
    public void Should_Complete_Observers_On_Dispose()
    {
        var buffer = new StudioLogBuffer();
        var observer = Substitute.For<IObserver<StudioLogEntry>>();

        using var subscription = buffer.Subscribe(observer);

        buffer.Dispose();

        observer.Received(1).OnCompleted();
    }

    [Fact]
    public void Should_Not_Propagate_And_Drop_Backlog_When_Backlog_Observer_Throws()
    {
        var buffer = new StudioLogBuffer();
        buffer.Add(CreateEntry(LogEventLevel.Information, "one"));
        buffer.Add(CreateEntry(LogEventLevel.Information, "two"));

        // The throwing subscriber's OnNext fires during backlog replay. Subscribe must not propagate
        // the exception (which would leave a half-attached subscription) and must drop the remaining
        // backlog so no stale entries are retained or replayed later.
        var firstSubscription = buffer.Subscribe(new ThrowingObserver());

        // The caller owns the returned subscription and detaches it when done; the buffer must not
        // auto-dispose it on replay failure.
        firstSubscription.Dispose();

        var live = new List<StudioLogEntry>();
        using var secondSubscription = buffer.Subscribe(live.Add);

        buffer.Add(CreateEntry(LogEventLevel.Information, "live"));

        // The backlog was dropped on failure, so the second subscriber only sees the live entry.
        live.Select(entry => entry.Message).ShouldBe(new[] { "live" });
    }

    private sealed class ThrowingObserver : IObserver<StudioLogEntry>
    {
        public void OnNext(StudioLogEntry value) => throw new InvalidOperationException("observer failure");

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private static StudioLogEntry CreateEntry(LogEventLevel level, string message)
    {
        return new StudioLogEntry(DateTimeOffset.Now, level, message, null);
    }
}
