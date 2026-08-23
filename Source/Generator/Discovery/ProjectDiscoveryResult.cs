namespace SlnDependencyDiagramGenerator.Generator.Discovery;

/// <summary>Represents the result of a lightweight project discovery operation.</summary>
public class ProjectDiscoveryResult
{
    /// <summary>All project paths discovered in the solution.</summary>
    public string[] AllProjectPaths { get; init; } = [];

    /// <summary>Project paths that matched the include regex and were not excluded.</summary>
    public string[] IncludedProjectPaths { get; init; } = [];

    /// <summary>Project paths that were excluded by the exclude regex.</summary>
    public string[] ExcludedProjectPaths { get; init; } = [];

    /// <summary>Project paths that did not match any include regex (implicit exclusions).</summary>
    public string[] ImplicitlyExcludedProjectPaths { get; init; } = [];
}