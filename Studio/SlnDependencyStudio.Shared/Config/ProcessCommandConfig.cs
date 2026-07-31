namespace SlnDependencyStudio.Shared.Config;

/// <summary>Base configuration for an optional command that executes as part of the generation pipeline.</summary>
public abstract class ProcessCommandConfig
{
    /// <summary>Whether the command is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>The command or executable path to run.</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>Command-line arguments passed to the command.</summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>The working directory for the command. Empty means the solution directory.</summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}
