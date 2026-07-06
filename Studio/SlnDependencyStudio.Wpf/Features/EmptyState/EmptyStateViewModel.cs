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
    private bool _hasRecentProjects;

    /// <summary>Recently opened project files, most recent first.</summary>
    public ObservableCollection<RecentProjectEntry> RecentProjects { get; } = [];

    /// <summary><see langword="true"/> when at least one recent project exists.</summary>
    public bool HasRecentProjects
    {
        get => _hasRecentProjects;
        set => this.RaiseAndSetIfChanged(ref _hasRecentProjects, value);
    }

    /// <summary>Command bound to the "New Project" card.</summary>
    public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }

    /// <summary>Command bound to the "Open Project" card.</summary>
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }

    /// <summary>Command bound to each recent-project row.</summary>
    public ReactiveCommand<string, Unit> OpenRecentProjectCommand { get; }

    /// <summary>Fired when the user requests a new project. The shell handles this.</summary>
    public Interaction<Unit, Unit> NewProjectRequested { get; } = new();

    /// <summary>Fired when the user requests to open a project. The shell handles this.</summary>
    public Interaction<Unit, Unit> OpenProjectRequested { get; } = new();

    /// <summary>Fired when the user clicks a recent project. The shell handles this.</summary>
    public Interaction<string, Unit> OpenRecentProjectRequested { get; } = new();

    /// <summary>Initializes a new instance of <see cref="EmptyStateViewModel"/>.</summary>
    /// <param name="recentProjectsService">Provides the list of recently opened project files.</param>
    public EmptyStateViewModel(IRecentProjectsService recentProjectsService)
    {
        NewProjectCommand = ReactiveCommand.CreateFromTask(
            async () => { await NewProjectRequested.Handle(Unit.Default); });

        OpenProjectCommand = ReactiveCommand.CreateFromTask(
            async () => { await OpenProjectRequested.Handle(Unit.Default); });

        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(
            async filePath => { await OpenRecentProjectRequested.Handle(filePath); });

        SetRecentProjects(recentProjectsService);
    }

    private void SetRecentProjects(IRecentProjectsService recentProjectsService)
    {
        foreach (var entry in recentProjectsService.GetRecent())
        {
            RecentProjects.Add(entry);
        }

        HasRecentProjects = RecentProjects.Count > 0;
    }
}
