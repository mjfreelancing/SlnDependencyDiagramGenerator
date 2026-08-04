using Serilog.Events;

namespace SlnDependencyStudio.Shared.Logging;

/// <summary>A single log event captured for display by a frontend (for example, the WPF output panel).</summary>
/// <param name="Timestamp">The timestamp of the log event.</param>
/// <param name="Level">The severity level of the log event.</param>
/// <param name="Message">The rendered message text.</param>
/// <param name="ExceptionText">The exception details, when present; otherwise <see langword="null"/>.</param>
public sealed record StudioLogEntry(DateTimeOffset Timestamp, LogEventLevel Level, string Message, string? ExceptionText);
