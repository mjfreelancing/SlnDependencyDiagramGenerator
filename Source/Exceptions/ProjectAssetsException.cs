namespace SlnDependencyDiagramGenerator.Exceptions;

/// <summary>
/// Exception thrown when a project's assets cannot be read — a missing
/// <c>project.assets.json</c> (run <c>dotnet restore</c>) or an unsupported assets format.
/// This is a distinct failure domain from generation-time failures so consumers can branch on
/// "fix your solution / restore problem".
/// </summary>
public sealed class ProjectAssetsException : DependencyGeneratorException
{
    /// <summary>Initializes a new project-assets exception.</summary>
    /// <param name="message">The error message describing the assets/restore problem.</param>
    public ProjectAssetsException(string message)
        : base(message)
    {
    }
}
