using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Represents a fully-resolved dependency graph for a single project scope (individual project or all-projects),
/// serving as the shared intermediate representation consumed by all diagram renderers.</summary>
internal sealed class DependencyGraphModel
{
    /// <summary>The ordered list of projects in this scope.</summary>
    public IReadOnlyList<ProjectNode> Projects { get; init; } = [];

    /// <summary>Package names (not aliases) that appear with more than one resolved version across the scope.</summary>
    public IReadOnlyDictionary<string, string> PackagesWithMultipleVersions { get; init; }
        = new Dictionary<string, string>();
}

    /// <summary>Represents a project node in a dependency graph.</summary>
internal sealed class ProjectNode
{
    /// <summary>The project name (no extension).</summary>
    public string Name { get; init; }

    /// <summary>Framework references declared by this project.</summary>
    public IReadOnlyList<FrameworkNode> FrameworkReferences { get; init; } = [];

    /// <summary>Direct (explicit) and transitive package references, as a recursive tree.</summary>
    public IReadOnlyList<PackageNode> PackageReferences { get; init; } = [];

    /// <summary>Other projects this project depends on, by name.</summary>
    public IReadOnlyList<string> ProjectReferences { get; init; } = [];
}

/// <summary>Represents a framework reference node in a dependency graph.</summary>
internal sealed class FrameworkNode
{
    /// <summary>The framework name.</summary>
    public string Name { get; init; }
}

/// <summary>Represents a package dependency node in a dependency graph.</summary>
internal sealed class PackageNode
{
    /// <summary>The package identifier.</summary>
    public string Name { get; init; }

    /// <summary>The resolved package version.</summary>
    public string Version { get; init; }

    /// <summary>Indicates whether the package is transitive.</summary>
    public bool IsTransitive { get; init; }

    /// <summary>The dependency depth (0 for explicit, greater than 0 for transitive).</summary>
    public int Depth { get; init; }

    /// <summary>Packages that this package depends on (up to the configured transitive depth).</summary>
    public IReadOnlyList<PackageNode> TransitiveReferences { get; init; } = [];
}
