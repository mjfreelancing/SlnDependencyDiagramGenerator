namespace SlnDependencyStudio.Shared.Exceptions;

/// <summary>
/// Exception thrown when a dependency project document cannot be deserialized — the content is
/// empty or not a valid dependency project document. Provides a clear, human-facing message so
/// frontends (CLI, WPF) can surface a meaningful error instead of a raw <see cref="NullReferenceException"/>.
/// </summary>
public sealed class DependencyProjectException : Exception
{
    /// <summary>Initializes a new dependency-project exception.</summary>
    /// <param name="message">The error message describing the invalid document.</param>
    public DependencyProjectException(string message)
        : base(message)
    {
    }
}
