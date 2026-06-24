namespace SlnDependencyStudio.Wpf.Features.Application.Models;

/// <summary>Durable application settings persisted to disk across sessions.</summary>
public sealed class ApplicationSettings
{
    /// <summary>The default folder for Open/Save file dialogs when no project is loaded.</summary>
    public string DefaultProjectFolder { get; set; } = string.Empty;

    /// <summary>Explicit path overrides for external tools (d2, mmdc).
    /// Key is the tool name, value is the full path to the executable.</summary>
    public Dictionary<string, string> ToolPathOverrides { get; set; } = [];

    /// <summary>Number of days to retain log files. Defaults to 30.</summary>
    public int LogRetentionDays { get; set; } = 30;
}
