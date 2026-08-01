using AllOverIt.Assertion;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.RecentProjects;

/// <summary>
/// Singleton store that owns the single <see cref="RecentProjects"/> collection.
/// </summary>
internal sealed class RecentProjectsStore : ReactiveObject, IRecentProjectsStore
{
    private readonly IRecentProjectsService _service;
    private readonly ILogger<RecentProjectsStore> _logger;
    private readonly ObservableCollectionExtended<RecentProjectEntry> _recentProjects = [];
    private readonly ObservableAsPropertyHelper<bool> _hasRecentProjects;

    /// <inheritdoc />
    public ObservableCollection<RecentProjectEntry> RecentProjects => _recentProjects;

    /// <inheritdoc />
    public bool HasRecentProjects => _hasRecentProjects.Value;

    /// <summary>Initializes a new instance of <see cref="RecentProjectsStore"/>.</summary>
    public RecentProjectsStore(IRecentProjectsService service, ILogger<RecentProjectsStore> logger)
    {
        _service = service.WhenNotNull();
        _logger = logger.WhenNotNull();

        _hasRecentProjects = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => RecentProjects.CollectionChanged += handler,
                handler => RecentProjects.CollectionChanged -= handler)
            .Select(_ => RecentProjects.Count > 0)
            .StartWith(RecentProjects.Count > 0)
            .ToProperty(this, nameof(HasRecentProjects));

        Refresh();
    }

    /// <inheritdoc />
    public void Add(string filePath)
    {
        _service.Add(filePath);
        Refresh();
    }

    /// <inheritdoc />
    public void Remove(string filePath)
    {
        _service.Remove(filePath);
        Refresh();
    }

    /// <inheritdoc />
    public void Refresh()
    {
        _recentProjects.Load(_service.GetRecent());

        _logger.LogDebug("Refreshed recent projects ({Count} entries)", _recentProjects.Count);
    }
}
