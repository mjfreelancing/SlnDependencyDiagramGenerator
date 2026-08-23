namespace SlnDependencyDiagramGenerator.Generator.ToolDetection;

/// <summary>Represents the availability status of a single external tool.</summary>
public sealed class ToolStatus
{
    /// <summary>The command/tool name.</summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>Whether the tool was found and is usable.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>The resolved executable path when found, or <see langword="null"/>.</summary>
    public string? ResolvedPath { get; init; }

    /// <summary>An error message when the tool is not available, or <see langword="null"/>.</summary>
    public string? ErrorMessage { get; init; }
}