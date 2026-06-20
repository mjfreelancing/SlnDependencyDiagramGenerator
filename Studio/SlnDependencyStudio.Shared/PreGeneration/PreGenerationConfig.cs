namespace SlnDependencyStudio.Shared.PreGeneration;

/// <summary>Configuration for an optional pre-generation command that executes before diagram generation starts.</summary>
public sealed class PreGenerationConfig
{
    /// <summary>Whether the pre-generation command is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>The command or executable path to run.</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>Command-line arguments passed to the pre-generation command.</summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>The working directory for the pre-generation command. Empty means the solution directory.</summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>When true, diagram generation proceeds even if the pre-generation command fails.</summary>
    public bool ContinueOnFailure { get; set; }
}