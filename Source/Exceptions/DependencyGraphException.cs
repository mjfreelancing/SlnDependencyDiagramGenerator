namespace SlnDependencyDiagramGenerator.Exceptions;

/// <summary>
/// Exception thrown when the project dependency graph is inconsistent — a referenced project is
/// not present in the discovered solution set (e.g. excluded by the include/exclude regexes) or a
/// circular project reference was detected. This is a distinct failure domain from parse/export
/// failures so consumers can branch on "your project graph / include-exclude configuration".
/// </summary>
public sealed class DependencyGraphException : DependencyGeneratorException
{
    /// <summary>Initializes a new dependency-graph exception.</summary>
    /// <param name="message">The error message describing the graph/configuration problem.</param>
    public DependencyGraphException(string message)
        : base(message)
    {
    }
}
