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
        return [.. _settingsService.CurrentState.RecentProjects
            .Select(filePath =>
            {
                var exists = File.Exists(filePath);
                return new RecentProjectEntry(filePath, Path.GetFileNameWithoutExtension(filePath), exists);
            })];
    }

    /// <inheritdoc />
    public void Remove(string filePath)
    {
        filePath.WhenNotNull();

        _settingsService.CurrentState.RecentProjects.Remove(filePath);
        _settingsService.SaveState();
    }
}
