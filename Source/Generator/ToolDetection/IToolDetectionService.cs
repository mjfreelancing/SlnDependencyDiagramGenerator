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

    /// <summary>Checks whether a specific tool is available. Consults the configured
    /// <see cref="IToolPathResolver"/> for an explicit path override, falling back to PATH lookup.</summary>
    /// <param name="toolName">The command/tool name to check (e.g. "d2", "mmdc").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating availability, resolved path, and any error message.</returns>
    Task<ToolStatus> CheckToolAvailabilityAsync(string toolName, CancellationToken cancellationToken = default);
}