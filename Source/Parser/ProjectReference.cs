namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents a project-to-project reference.</summary>
public sealed class ProjectReference
{
    /// <summary>The referenced project path.</summary>
    public string Path { get; init; } = string.Empty;
}