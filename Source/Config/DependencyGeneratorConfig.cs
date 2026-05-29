namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Provides configuration options that specify which projects in a solution are processed,
/// how dependency depth and diagram styling are applied, and where diagram/source outputs are exported.</summary>
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