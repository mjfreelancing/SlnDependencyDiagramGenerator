using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Subjects;

namespace SlnDependencyDiagramGenerator.Generator;

/// <inheritdoc cref="IProgressReporter"/>
internal sealed class ProgressReporter : IProgressReporter
{
    private readonly Subject<string> _subject = new();

    /// <inheritdoc />
    public IObservable<string> OnProgress => _subject;

    /// <inheritdoc />
    public void Report(string message, ILogger logger)
    {
        // :l = literal — renders the string value without quotes so the log file
        // and output panel display the message verbatim (no wrapping " chars).
        logger.LogInformation("{ProgressMessage:l}", message);
        _subject.OnNext(message);
    }
}
