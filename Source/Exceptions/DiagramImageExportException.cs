namespace SlnDependencyDiagramGenerator.Exceptions;

/// <summary>Exception thrown when an external diagram tool fails to export an image for a generated diagram.</summary>
public sealed class DiagramImageExportException : DependencyGeneratorException
{
    /// <summary>Initializes a new diagram image export exception.</summary>
    /// <param name="message">The error message describing the failed export.</param>
    public DiagramImageExportException(string message)
        : base(message)
    {
    }
}
