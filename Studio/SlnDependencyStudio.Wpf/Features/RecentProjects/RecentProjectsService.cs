using AllOverIt.Assertion;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.RecentProjects;

internal sealed class RecentProjectsService : IRecentProjectsService
{
    private const int MaxEntries = 10;

    private readonly IApplicationSettingsService _settingsService;

    public RecentProjectsService(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService.WhenNotNull();
    }

    /// <inheritdoc />
    public void Add(string filePath)
    {
        filePath.WhenNotNull();

        var recentProjects = _settingsService.CurrentState.RecentProjects;

        recentProjects.Remove(filePath);
        recentProjects.Insert(0, filePath);

        if (recentProjects.Count > MaxEntries)
        {
            recentProjects.RemoveRange(MaxEntries, recentProjects.Count - MaxEntries);
        }

        _settingsService.SaveState();
    }

    /// <inheritdoc />
    public RecentProjectEntry[] GetRecent()
    {
        var entries = _settingsService.CurrentState.RecentProjects
            .Where(File.Exists)
            .Select(filePath => new RecentProjectEntry(filePath, Path.GetFileNameWithoutExtension(filePath)))
            .ToArray();

        // Prune any entries that no longer exist on disk.
        if (entries.Length != _settingsService.CurrentState.RecentProjects.Count)
        {
            _settingsService.CurrentState.RecentProjects.RemoveAll(
                path => entries.All(entry => entry.FilePath != path));

            _settingsService.SaveState();
        }

        return entries;
    }

    /// <inheritdoc />
    public void Remove(string filePath)
    {
        filePath.WhenNotNull();

        _settingsService.CurrentState.RecentProjects.Remove(filePath);
        _settingsService.SaveState();
    }
}
