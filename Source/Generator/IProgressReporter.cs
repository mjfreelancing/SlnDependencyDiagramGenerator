using Microsoft.Extensions.Logging;
using System;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>
/// A singleton service that provides a shared progress-reporting channel.
/// Call <see cref="Report"/> to log an informational message and simultaneously
/// push it to <see cref="OnProgress"/> for live UI streaming.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Streams all progress messages reported via <see cref="Report"/>.
    /// </summary>
    IObservable<string> OnProgress { get; }

    /// <summary>
    /// Logs <paramref name="message"/> as <see cref="LogLevel.Information"/> via
    /// <paramref name="logger"/> and pushes it to <see cref="OnProgress"/>.
    /// </summary>
    void Report(string message, ILogger logger);
}
