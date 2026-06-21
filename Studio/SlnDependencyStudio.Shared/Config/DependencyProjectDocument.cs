using SlnDependencyDiagramGenerator.Config;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Shared.Config;

/// <summary>The root document model for a saved dependency project file.
/// Uses an extensible envelope so future metadata can be added without breaking existing files.</summary>
public sealed class DependencyProjectDocument
{
    /// <summary>The schema version of this document. Used for migration and forward compatibility.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>User-facing metadata about the project.</summary>
    public DependencyProjectMetadata Metadata { get; init; } = new();

    /// <summary>The diagram generator configuration payload.</summary>
    public DependencyGeneratorConfig DiagramGenerator { get; init; } = new();

    /// <summary>Optional pre-generation command configuration.</summary>
    public PreGenerationConfig PreGeneration { get; init; } = new();

    /// <summary>Captures unknown JSON fields for forward compatibility.
    /// Fields not matching known properties are stored here and re-serialized on save,
    /// so editing a document with a newer schema version does not strip unknown data.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; init; }
}
