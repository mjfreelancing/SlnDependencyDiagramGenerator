using SlnDependencyDiagramGenerator.Config;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>Provides detection and availability checking for external CLI tools required by diagram renderers.</summary>
public interface IToolDetectionService
{
    /// <summary>The distinct set of tool names known to this service (e.g. "d2", "mmdc").</summary>
    IReadOnlyList<string> KnownToolNames { get; }

    /// <summary>Checks whether a specific tool is available on PATH or at an explicit path.</summary>
    /// <param name="toolName">The command/tool name to check (e.g. "d2", "mmdc").</param>
    /// <param name="explicitPath">An optional explicit path to the tool executable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating availability, resolved path, and any error message.</returns>
    Task<ToolStatus> CheckToolAvailabilityAsync(string toolName, string? explicitPath = null, CancellationToken cancellationToken = default);

    /// <summary>Checks availability for all tools required by the configured image formats.</summary>
    /// <param name="diagramFormats">The configured diagram export formats.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A readiness result covering all required tools.</returns>
    Task<ToolReadinessResult> CheckConfiguredToolsAsync(DiagramFormat[] diagramFormats, CancellationToken cancellationToken);
}