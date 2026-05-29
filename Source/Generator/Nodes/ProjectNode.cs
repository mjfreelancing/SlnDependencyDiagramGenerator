namespace SlnDependencyDiagramGenerator.Generator.Nodes;

/// <summary>Represents a project node in a dependency graph.</summary>
internal sealed class ProjectNode
{
    /// <summary>The project name (no extension).</summary>
    public string Name { get; init; }

    /// <summary>Framework references declared by this project.</summary>
    public FrameworkNode[] FrameworkReferences { get; init; } = [];

    /// <summary>Direct (explicit) and transitive package references, as a recursive tree.</summary>
    public PackageNode[] PackageReferences { get; init; } = [];

    /// <summary>Other projects this project depends on, by name.</summary>
    public string[] ProjectReferences { get; init; } = [];
}
