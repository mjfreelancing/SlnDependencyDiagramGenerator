namespace SlnDependencyStudio.Wpf.Features.RecentProjects.Models;

/// <summary>A display entry for the recent projects list.</summary>
/// <param name="FilePath">The full path to the <c>.sds</c> file.</param>
/// <param name="DisplayName">The file name without extension, for display.</param>
public sealed record RecentProjectEntry(string FilePath, string DisplayName);
