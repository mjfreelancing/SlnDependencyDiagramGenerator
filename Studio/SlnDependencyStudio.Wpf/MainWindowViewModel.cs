using AllOverIt.Assertion;
using AllOverIt.ReactiveUI;
using AllOverIt.ReactiveUI.Factories;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf;

/// <summary>View model for the main application shell window.</summary>
public sealed class MainWindowViewModel : ActivatableViewModel
{
    private ObservableAsPropertyHelper<bool> _hasValidationSummaryItems = null!;
    private readonly IDependencyProjectService _projectService;
    private readonly IViewFactory _viewFactory;

    /// <summary>The navigation items displayed in the left sidebar.</summary>
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    /// <summary>The currently selected navigation item.</summary>
    [Reactive]
    public NavigationItemViewModel? SelectedNavigationItem { get; set; }

    /// <summary>The view model for the current centre workspace page.</summary>
    [Reactive]
    public object? CurrentPage { get; set; }

    /// <summary>The currently loaded project, or <see langword="null"/> if none is open.</summary>
    [Reactive]
    public DependencyProjectViewModel? CurrentProject { get; set; }

    /// <summary>Validation errors collected across all editable sections.</summary>
    public ObservableCollection<ValidationSummaryItem> CurrentValidationSummary { get; } = [];

    /// <summary>Whether the main window can be closed.</summary>
    [Reactive]
    public bool CanClose { get; set; } = true;

    /// <summary>Whether a generation run is currently in progress.</summary>
    [Reactive]
    public bool IsGenerating { get; set; }

    /// <summary><see langword="true"/> when the validation summary bar should be visible.</summary>
    public bool HasValidationSummaryItems => _hasValidationSummaryItems.Value;

    /// <summary>Command that opens the application settings dialog.</summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

    /// <summary>Command that opens an existing <c>.sds</c> project file.</summary>
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }

    /// <summary>Interaction for showing an open-file dialog. Returns the selected path or <see langword="null"/>.</summary>
    public Interaction<string, string?> OpenFileInteraction { get; } = new();

    /// <summary>Command that closes the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel(IDependencyProjectService projectService, IViewFactory viewFactory)
    {
        _projectService = projectService;
        _viewFactory = viewFactory;

        OpenSettingsCommand = ReactiveCommand.Create(() => { });

        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);

        ExitCommand = ReactiveCommand.Create(() => { });

        // Populate navigation items.
        NavigationItems =
        [
            new NavigationItemViewModel<ProjectViewModel>
            {
                DisplayName = "Project",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.FileDocumentOutline,
                ConfigureViewModel = viewModel =>
                {
                    Throw<InvalidOperationException>.WhenNull(CurrentProject, "The current project has not been assigned");
                    ((ProjectViewModel)viewModel).LoadFrom(CurrentProject.Document);
                }
            }
        ];
    }

    /// <inheritdoc />
    protected override void OnActivated(CompositeDisposable disposables)
    {
        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => CurrentValidationSummary.CollectionChanged += handler,
                handler => CurrentValidationSummary.CollectionChanged -= handler)
            .Select(_ => CurrentValidationSummary.Count > 0)
            .StartWith(CurrentValidationSummary.Count > 0)
            .ToProperty(this, vm => vm.HasValidationSummaryItems, out _hasValidationSummaryItems)
            .DisposeWith(disposables);

        // Navigate to the page view when a nav item is selected.
        this.WhenAnyValue(vm => vm.SelectedNavigationItem)
            .Where(item => item is not null && CurrentProject is not null)
            .Subscribe(item => NavigateToPage(item!))
            .DisposeWith(disposables);
    }

    private async Task OpenProjectAsync()
    {
        var filePath = await OpenFileInteraction.Handle(
            "SlnDependencyStudio project files (*.sds)|*.sds|All files (*.*)|*.*");

        if (filePath is null)
        {
            return;
        }

        var document = await _projectService.OpenAsync(filePath);
        var projectViewModel = new DependencyProjectViewModel(document, filePath);

        CurrentProject = projectViewModel;

        // Navigate to the Project page - will trigger the subscription attached to SelectedNavigationItem.
        SelectNavigationItem<ProjectViewModel>();
    }

    /// <summary>Selects the navigation item whose <see cref="NavigationItemViewModel.ViewModelType"/>
    /// matches <typeparamref name="TViewModel"/>, which triggers the nav subscription to show the
    /// corresponding page view via <see cref="NavigateToPage"/>.</summary>
    /// <typeparam name="TViewModel">The page view model type to select.</typeparam>
    private void SelectNavigationItem<TViewModel>()
    {
        // Will result in NavigateToPage() being called via the SelectedNavigationItem subscription.
        SelectedNavigationItem = NavigationItems.Single(item => item.ViewModelType == typeof(TViewModel));
    }

    /// <summary>Navigates to the workspace page corresponding to the selected navigation item.</summary>
    private void NavigateToPage(NavigationItemViewModel item)
    {
        // CreateView() will invoke the item's ConfigureViewModel action
        CurrentPage = item.CreateView(_viewFactory);
    }
}
