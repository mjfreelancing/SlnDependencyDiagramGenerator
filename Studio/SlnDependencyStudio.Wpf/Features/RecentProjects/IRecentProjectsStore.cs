using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using System.Collections.ObjectModel;

namespace SlnDependencyStudio.Wpf.Features.RecentProjects;

/// <summary>
/// Singleton store that owns the single <see cref="RecentProjects"/> collection.
/// </summary>
public interface IRecentProjectsStore : IStudioSingletonDependency
{
    /// <summary>The single collection of recent project entries, shared across all consumers.</summary>
    ObservableCollection<RecentProjectEntry> RecentProjects { get; }

    /// <summary><see langword="true"/> when at least one recent project exists.</summary>
    bool HasRecentProjects { get; }

    /// <summary>Adds a file path to the top of the list and refreshes the collection.</summary>
    void Add(string filePath);

    /// <summary>Removes a file path from the list and refreshes the collection.</summary>
    void Remove(string filePath);

    /// <summary>Re-reads the persisted list from the service and repopulates the collection.</summary>
    void Refresh();
}
