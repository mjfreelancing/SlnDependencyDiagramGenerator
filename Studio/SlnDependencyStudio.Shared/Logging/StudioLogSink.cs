using Serilog.Core;
using Serilog.Events;

namespace SlnDependencyStudio.Shared.Logging;

/// <summary>A Serilog sink that forwards rendered log events to an <see cref="IStudioLogBuffer"/>.</summary>
/// <remarks>This sink never logs itself, avoiding recursion back into the Serilog pipeline.</remarks>
public sealed class StudioLogSink : ILogEventSink
{
    private readonly IStudioLogBuffer _buffer;

    /// <summary>Initializes a new instance of the <see cref="StudioLogSink"/> class.</summary>
    /// <param name="buffer">The log buffer to forward events to.</param>
    public StudioLogSink(IStudioLogBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _buffer = buffer;
    }

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        _buffer.Add(new StudioLogEntry(
            logEvent.Timestamp,
            logEvent.Level,
            logEvent.RenderMessage(),
            logEvent.Exception?.ToString()));
    }
}
