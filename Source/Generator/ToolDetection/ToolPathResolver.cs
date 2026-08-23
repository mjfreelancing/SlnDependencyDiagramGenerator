using SlnDependencyDiagramGenerator.Config;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>
/// Resolves the effective tool path for diagram renderers and availability checks.
/// Calls the configured <see cref="ToolPathOverridesProvider"/> on every lookup so that
/// tool path changes in application settings are reflected immediately without push updates.
/// </summary>
internal sealed class ToolPathResolver : IToolPathResolver
{
    private static readonly Dictionary<DiagramFormat, string> ToolNames = new()
    {
        [DiagramFormat.D2] = "d2",
        [DiagramFormat.Mermaid] = "mmdc"
    };

    private readonly ToolPathOverridesProvider _overridesProvider;
    private Dictionary<string, string> PathOverrides => _overridesProvider.Invoke();

    /// <summary>Initializes a new resolver with the provided overrides source.</summary>
    /// <param name="overridesProvider">A delegate that returns the current set of explicit path overrides.</param>
    public ToolPathResolver(ToolPathOverridesProvider overridesProvider)
    {
        _overridesProvider = overridesProvider;
    }

    /// <summary>Gets the effective path to use for tool invocation.
    /// Returns the explicit override if configured, otherwise the tool name for PATH lookup.</summary>
    /// <param name="format">The diagram format.</param>
    public string GetEffectivePath(DiagramFormat format)
    {
        var toolName = ToolNames[format];

        return PathOverrides.TryGetValue(toolName, out var path) ? path : toolName;
    }

    /// <summary>Gets the explicit path override for a tool, or <see langword="null"/> if not configured.</summary>
    /// <param name="format">The diagram format.</param>
    public string? GetExplicitPath(DiagramFormat format)
    {
        var toolName = ToolNames[format];

        return PathOverrides.GetValueOrDefault(toolName);
    }

    /// <inheritdoc />
    public string? GetExplicitPath(string toolName)
    {
        return PathOverrides.GetValueOrDefault(toolName);
    }

    /// <summary>Gets the tool name for a diagram format (e.g. "d2", "mmdc").</summary>
    /// <param name="format">The diagram format.</param>
    public string GetToolName(DiagramFormat format) => ToolNames[format];
}
