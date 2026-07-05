using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Features.Application.Models;

/// <summary>Durable application settings persisted to disk across sessions.</summary>
public sealed class ApplicationSettings
{
    internal const int DefaultLogRetentionDays = 31;

    /// <summary>The default folder for Open/Save file dialogs when no project is loaded.</summary>
    public string DefaultProjectFolder { get; set; } = string.Empty;

    /// <summary>Explicit path overrides for external tools (d2, mmdc).
    /// Key is the tool name, value is the full path to the executable.</summary>
    public Dictionary<string, string> ToolPathOverrides { get; set; } = [];

    /// <summary>Number of days to retain log files. Defaults to 31.</summary>
    public int LogRetentionDays { get; set; } = DefaultLogRetentionDays;

    /// <summary>The application theme. Defaults to <see cref="StudioTheme.Light"/>.</summary>
    public StudioTheme Theme { get; set; } = StudioTheme.Light;

    public ApplicationSettings Clone()
    {
        return new ApplicationSettings
        {
            DefaultProjectFolder = DefaultProjectFolder,
            ToolPathOverrides = new Dictionary<string, string>(ToolPathOverrides),
            LogRetentionDays = LogRetentionDays,
            Theme = Theme
        };
    }
}
