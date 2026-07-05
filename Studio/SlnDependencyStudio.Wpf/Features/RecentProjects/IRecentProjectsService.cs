using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;

namespace SlnDependencyStudio.Wpf.Features.RecentProjects;

/// <summary>Manages the list of recently opened dependency project files.</summary>
public interface IRecentProjectsService : IStudioSingletonDependency
{
    /// <summary>Adds or moves a file path to the top of the recent projects list.
    /// Duplicates are moved, not duplicated. The list is capped at 10 entries.</summary>
    /// <param name="filePath">The full path to the <c>.sds</c> file.</param>
    void Add(string filePath);

    /// <summary>Returns the current list of recent project entries, most recent first.</summary>
    /// <returns>A read-only list of recent project entries.</returns>
    RecentProjectEntry[] GetRecent();

    /// <summary>Removes a file path from the recent projects list.</summary>
    /// <param name="filePath">The full path to remove.</param>
    void Remove(string filePath);
}
