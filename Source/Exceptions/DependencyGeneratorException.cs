using System;

namespace SlnDependencyDiagramGenerator.Exceptions;

/// <summary>The exception type raised when an error occurs while processing the dependency diagram generator.</summary>
public class DependencyGeneratorException : Exception
{
    /// <summary>Initializes a new dependency generator exception.</summary>
    /// <param name="message">The error message.</param>
    public DependencyGeneratorException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new dependency generator exception with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception that caused this error.</param>
    public DependencyGeneratorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}