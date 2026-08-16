using SlnDependencyDiagramGenerator.Config;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Shared.Config;

/// <summary>The root document model for a saved dependency project file.
/// Uses an extensible envelope so future metadata can be added without breaking existing files.</summary>
public sealed class DependencyProjectDocument
{
    /// <summary>The schema version of this document. Used for migration and forward compatibility.
    /// Settable so a schema migration step can advance it to the next version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>User-facing metadata about the project.</summary>
    public DependencyProjectMetadata Metadata { get; init; } = new();

    /// <summary>The diagram generator configuration payload.</summary>
    public DependencyGeneratorConfig DiagramGenerator { get; init; } = new();

    /// <summary>Optional pre-generation command configuration.</summary>
    public PreGenerationConfig PreGeneration { get; init; } = new();

    /// <summary>Whether the solution should be restored (via <c>dotnet restore</c>) before generation starts.</summary>
    public bool RestoreSolution { get; set; } = true;

    /// <summary>Optional post-generation command configuration.</summary>
    public PostGenerationConfig PostGeneration { get; init; } = new();

    /// <summary>Captures unknown JSON fields for forward compatibility.
    /// Fields not matching known properties are stored here and re-serialized on save, so a field
    /// added by a newer build without a schema-version bump is not stripped on round-trip. Documents
    /// with a newer schema version are rejected on load (update the application to open them), so
    /// this never preserves fields from an unsupported schema.</summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; init; }
}
