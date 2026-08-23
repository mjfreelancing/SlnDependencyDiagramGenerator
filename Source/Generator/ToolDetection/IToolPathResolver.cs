using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>
/// Resolves the effective tool path for diagram renderers and availability checks.
/// Consults the configured <see cref="ToolPathOverridesProvider"/> on every lookup,
/// so tool path changes in application settings are reflected immediately.
/// </summary>
public interface IToolPathResolver
{
    /// <summary>Gets the effective path to use for tool invocation.
    /// Returns the explicit override if configured, otherwise the tool name for PATH lookup.</summary>
    /// <param name="format">The diagram format.</param>
    string GetEffectivePath(DiagramFormat format);

    /// <summary>Gets the explicit path override for a tool, or <see langword="null"/> if not configured.</summary>
    /// <param name="format">The diagram format.</param>
    string? GetExplicitPath(DiagramFormat format);

    /// <summary>Gets the explicit path override for a tool by name, or <see langword="null"/> if not configured.</summary>
    /// <param name="toolName">The tool name (e.g. "d2", "mmdc").</param>
    string? GetExplicitPath(string toolName);

    /// <summary>Gets the tool name for a diagram format (e.g. "d2", "mmdc").</summary>
    /// <param name="format">The diagram format.</param>
    string GetToolName(DiagramFormat format);
}
