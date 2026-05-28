namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Provides configuration options that specify which projects for a specified solution are parsed,
/// where each project nuget dependency is resolved from, how deep the dependency graph is resolved, how the diagram
/// will be styled, and where the diagrams and images will be exported to.</summary>
public sealed class DependencyGeneratorConfig
{
    /// <summary>Specifies project related options that determine which projects for a given solution
    /// are resolved and the depth of their package dependency graph.</summary>
    public GeneratorProjectOptions Projects { get; init; } = new GeneratorProjectOptions();

    /// <summary>Specifies diagram options that determine how the diagram will be styled.</summary>
    public GeneratorDiagramOptions Diagram { get; init; } = new GeneratorDiagramOptions();

    /// <summary>Specifies export path and image format options.</summary>
    public GeneratorExportOptions Export { get; init; } = new GeneratorExportOptions();
}