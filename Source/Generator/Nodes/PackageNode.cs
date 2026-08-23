namespace SlnDependencyDiagramGenerator.Generator.Nodes;

/// <summary>Represents a package dependency node in a dependency graph.</summary>
internal sealed class PackageNode
{
    /// <summary>The package identifier.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The resolved package version.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Indicates whether the package is transitive.</summary>
    public bool IsTransitive { get; init; }

    /// <summary>The dependency depth (0 for explicit, greater than 0 for transitive).</summary>
    public int Depth { get; init; }

    /// <summary>Packages that this package depends on (up to the configured transitive depth).</summary>
    public PackageNode[] TransitiveReferences { get; init; } = [];
}
