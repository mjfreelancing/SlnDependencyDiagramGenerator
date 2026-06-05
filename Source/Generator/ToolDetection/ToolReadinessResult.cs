using System.Linq;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>Represents the overall readiness of required external tools.</summary>
public sealed class ToolReadinessResult
{
    /// <summary>The status for each checked tool.</summary>
    public ToolStatus[] ToolStatuses { get; init; } = [];

    /// <summary>Whether all required tools are available.</summary>
    public bool AllRequiredToolsAvailable => ToolStatuses.All(status => status.IsAvailable);
}