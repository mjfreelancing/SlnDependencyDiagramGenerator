using SlnDependencyDiagramGenerator.Generator.Nodes;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Represents a fully-resolved dependency graph for a single project scope (individual project or all-projects),
/// serving as the shared intermediate representation consumed by all diagram renderers.</summary>
internal sealed class DependencyGraphModel
{
    /// <summary>The ordered list of projects in this scope.</summary>
    public ProjectNode[] Projects { get; init; } = [];

    /// <summary>Package names that resolve to more than one version across the scope, mapped to the
    /// diagram group id used to group their nodes.</summary>
    public IReadOnlyDictionary<string, string> PackagesWithMultipleVersions { get; init; } = new Dictionary<string, string>();
}
