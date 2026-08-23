namespace SlnDependencyStudio.Wpf.Features.Application.Models;

/// <summary>Transient application state persisted across sessions but not considered
/// user-editable settings (recent files, window placement).</summary>
public sealed class ApplicationState
{
    /// <summary>Recently opened dependency project file paths, most recent first.</summary>
    public List<string> RecentProjects { get; set; } = [];

    /// <summary>Last-known main window placement and state.</summary>
    public WindowPlacement? WindowPlacement { get; set; }
}
