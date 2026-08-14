namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Provides configuration options that specify which projects in a solution are processed,
/// how dependency depth and diagram styling are applied, and where diagram/source outputs are exported.</summary>
public sealed class DependencyGeneratorConfig
{
    /// <summary>Specifies solution-related options: solution path, filters, exclusions, and scope configuration.</summary>
    public GeneratorSolutionOptions Solution { get; init; } = new GeneratorSolutionOptions();

    /// <summary>Specifies diagram options that determine how the diagram will be styled.</summary>
    public GeneratorDiagramOptions Diagram { get; init; } = new GeneratorDiagramOptions();

    /// <summary>Specifies export path and image format options.</summary>
    public GeneratorExportOptions Export { get; init; } = new GeneratorExportOptions();
}