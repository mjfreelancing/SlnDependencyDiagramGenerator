using Microsoft.Extensions.Logging;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Support;

/// <summary>
/// An <see cref="ILogger{T}"/> that captures every log call (level, formatted message, and exception)
/// so tests can assert on what a service logged.
/// </summary>
/// <typeparam name="T">The logger category type.</typeparam>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>A single captured log call.</summary>
    /// <param name="Level">The log level.</param>
    /// <param name="Message">The rendered (formatted) message.</param>
    /// <param name="Exception">The exception passed to the log call, if any.</param>
    public sealed record LogRecord(LogLevel Level, string Message, Exception? Exception);

    private readonly List<LogRecord> _records = [];

    /// <summary>The log calls captured so far, in the order they were made.</summary>
    public IReadOnlyList<LogRecord> Records => _records;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _records.Add(new LogRecord(logLevel, formatter(state, exception), exception));
    }
}
