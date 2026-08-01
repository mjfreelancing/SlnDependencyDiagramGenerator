using AllOverIt.Assertion;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.RecentProjects;

internal sealed class RecentProjectsService : IRecentProjectsService
{
    private const int MaxEntries = 10;

    private readonly IFileSystem _fileSystem;
    private readonly IApplicationSettingsService _settingsService;
    private readonly ILogger<RecentProjectsService> _logger;

    public RecentProjectsService(IFileSystem fileSystem, IApplicationSettingsService settingsService,
        ILogger<RecentProjectsService> logger)
    {
        _fileSystem = fileSystem.WhenNotNull();
        _settingsService = settingsService.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public void Add(string filePath)
    {
        filePath.WhenNotNull();

        _logger.LogDebug("Adding recent project: {FilePath}", filePath);

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
                var exists = _fileSystem.FileExists(filePath);
                return new RecentProjectEntry(filePath, Path.GetFileNameWithoutExtension(filePath), exists);
            })];
    }

    /// <inheritdoc />
    public void Remove(string filePath)
    {
        filePath.WhenNotNull();

        _logger.LogDebug("Removing recent project: {FilePath}", filePath);

        _settingsService.CurrentState.RecentProjects.Remove(filePath);
        _settingsService.SaveState();
    }
}
