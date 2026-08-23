namespace SlnDependencyDiagramGenerator.Generator.Nodes;

/// <summary>Represents a project node in a dependency graph.</summary>
internal sealed class ProjectNode
{
    /// <summary>The project name (no extension).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Framework references declared by this project.</summary>
    public FrameworkNode[] FrameworkReferences { get; init; } = [];

    /// <summary>Direct (explicit) and transitive package references, as a recursive tree.</summary>
    public PackageNode[] PackageReferences { get; init; } = [];

    /// <summary>The project file paths this project depends on.</summary>
    public string[] ProjectReferences { get; init; } = [];
}
