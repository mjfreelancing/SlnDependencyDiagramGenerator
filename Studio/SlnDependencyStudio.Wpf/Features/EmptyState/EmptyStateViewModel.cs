using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.EmptyState;

/// <summary>View model for the empty-state landing page shown when no project is loaded.</summary>
public sealed class EmptyStateViewModel : ReactiveObject
{
    /// <summary>Recently opened project files, most recent first. Shared collection from the store.</summary>
    public ObservableCollection<RecentProjectEntry> RecentProjects { get; }

    /// <summary><see langword="true"/> when at least one recent project exists.</summary>
    public bool HasRecentProjects => _recentProjectsStore.HasRecentProjects;

    /// <summary>Command bound to the "New Project" card.</summary>
    public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }

    /// <summary>Command bound to the "Open Project" card.</summary>
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }

    /// <summary>Command bound to each recent-project row.</summary>
    public ReactiveCommand<string, Unit> OpenRecentProjectCommand { get; }

    /// <summary>Command bound to the ✕ button on missing recent-project rows.</summary>
    public ReactiveCommand<string, Unit> RemoveRecentProjectCommand { get; }

    /// <summary>Fired when the user requests a new project. The shell handles this.</summary>
    public Interaction<Unit, Unit> NewProjectRequested { get; } = new();

    /// <summary>Fired when the user requests to open a project. The shell handles this.</summary>
    public Interaction<Unit, Unit> OpenProjectRequested { get; } = new();

    /// <summary>Fired when the user clicks a recent project. The shell handles this.</summary>
    public Interaction<string, Unit> OpenRecentProjectRequested { get; } = new();

    private readonly IRecentProjectsStore _recentProjectsStore;

    /// <summary>Initializes a new instance of <see cref="EmptyStateViewModel"/>.</summary>
    /// <param name="recentProjectsStore">The shared store that owns the recent projects collection.</param>
    public EmptyStateViewModel(IRecentProjectsStore recentProjectsStore)
    {
        _recentProjectsStore = recentProjectsStore;
        RecentProjects = _recentProjectsStore.RecentProjects;

        NewProjectCommand = ReactiveCommand.CreateFromTask(
            async () => { await NewProjectRequested.Handle(Unit.Default); });

        OpenProjectCommand = ReactiveCommand.CreateFromTask(
            async () => { await OpenProjectRequested.Handle(Unit.Default); });

        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(
            async filePath => { await OpenRecentProjectRequested.Handle(filePath); });

        RemoveRecentProjectCommand = ReactiveCommand.Create<string>(filePath =>
        {
            _recentProjectsStore.Remove(filePath);
        });
    }
}
