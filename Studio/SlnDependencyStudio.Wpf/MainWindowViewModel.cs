using AllOverIt.Extensions;   // IsNotNullOrEmpty
using AllOverIt.ReactiveUI;
using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Models;
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
    const string StudioFilesFilter = "Studio Project files (*.sds)|*.sds|All files (*.*)|*.*";

    private ObservableAsPropertyHelper<bool> _hasValidationSummaryItems = null!;
    private readonly IProjectDocumentStore _store;
    private readonly IDependencyProjectService _projectService;
    private readonly IViewFactory _viewFactory;
    private readonly ILogger<MainWindowViewModel> _logger;

    /// <summary>The navigation items displayed in the left sidebar.</summary>
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    /// <summary>The currently selected navigation item.</summary>
    [Reactive]
    public NavigationItemViewModel? SelectedNavigationItem { get; set; }

    /// <summary>The view for the current centre workspace page.</summary>
    [Reactive]
    public object? CurrentPage { get; set; }

    /// <summary>Validation errors collected across all editable sections.</summary>
    public ObservableCollection<ValidationSummaryItem> CurrentValidationSummary { get; } = [];

    /// <summary>Whether the main window can be closed.</summary>
    [Reactive]
    public bool CanClose { get; set; } = true;

    /// <summary><see langword="true"/> when a document is currently loaded in the store.</summary>
    [ObservableAsProperty]
    public bool HasDocument { get; }

    /// <summary>Whether a generation run is currently in progress.</summary>
    [Reactive]
    public bool IsGenerating { get; set; }

    /// <summary><see langword="true"/> when the validation summary bar should be visible.</summary>
    public bool HasValidationSummaryItems => _hasValidationSummaryItems.Value;

    /// <summary>Command that opens the application settings dialog.</summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

    /// <summary>Command that opens an existing <c>.sds</c> project file.</summary>
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }

    /// <summary>Command that creates a new project from defaults.</summary>
    public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }

    /// <summary>Command that creates a new project by loading an existing <c>.sds</c> file as a starting point.</summary>
    public ReactiveCommand<Unit, Unit> NewFromExistingCommand { get; }

    /// <summary>Command that saves the current project via the store.</summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Command that saves the current project to a new file path.</summary>
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    /// <summary>Command that closes the current project.</summary>
    public ReactiveCommand<Unit, Unit> CloseProjectCommand { get; }

    /// <summary>Interaction for showing an open-file dialog. Returns the selected path or <see langword="null"/>.</summary>
    public Interaction<string, string?> OpenFileInteraction { get; } = new();

    /// <summary>Interaction for showing a save-file dialog. Returns the selected path or <see langword="null"/>.</summary>
    public Interaction<string, string?> SaveFileInteraction { get; } = new();

    /// <summary>Interaction for showing a save-before-discard confirmation dialog.</summary>
    public Interaction<string, DiscardAction> ConfirmDiscardInteraction { get; } = new();

    /// <summary>Command that closes the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel(IProjectDocumentStore store, IDependencyProjectService projectService, IViewFactory viewFactory,
        ILogger<MainWindowViewModel> logger)
    {
        _store = store;
        _projectService = projectService;
        _viewFactory = viewFactory;
        _logger = logger;

        OpenSettingsCommand = ReactiveCommand.Create(() => { });
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectAsync);
        NewFromExistingCommand = ReactiveCommand.CreateFromTask(NewFromExistingAsync);
        SaveCommand = CreateSaveCommand();
        SaveAsCommand = CreateSaveAsCommand();
        CloseProjectCommand = CreateCloseProjectCommand();
        ExitCommand = ReactiveCommand.Create(() => { });

        // Populate navigation items. Each page VM receives the store via DI and self-initialises.
        NavigationItems =
        [
            new NavigationItemViewModel<ProjectViewModel>
            {
                DisplayName = "Project",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.FileDocumentOutline
            }
        ];
    }

    /// <inheritdoc />
    protected override void OnActivated(CompositeDisposable disposables)
    {
        WireDocumentStateTracking(disposables);
        WireSaveCommandLogging(disposables);
        WireValidationTracking(disposables);
        WireNavigation(disposables);
    }

    private void WireDocumentStateTracking(CompositeDisposable disposables)
    {
        _store.WhenAnyValue(store => store.HasDocument)
            .ToPropertyEx(this, vm => vm.HasDocument)
            .DisposeWith(disposables);

        _store
            .WhenAnyValue(store => store.CurrentFilePath)
            .Subscribe(filePath =>
            {
                if (filePath is null)
                {
                    _logger.LogInformation("No project is currently loaded");
                }
                else
                {
                    _logger.LogInformation("Current project: {FilePath}", filePath);
                }
            })
            .DisposeWith(disposables);

        _store
            .WhenAnyValue(store => store.IsDirty)
            .Subscribe(isDirty =>
            {
                if (isDirty)
                {
                    _logger.LogInformation("Current project has unsaved changes");
                }
                else
                {
                    _logger.LogInformation("Current project is clean (no unsaved changes)");
                }
            })
            .DisposeWith(disposables);
    }

    private void WireSaveCommandLogging(CompositeDisposable disposables)
    {
        SaveCommand.CanExecute
            .Subscribe(enabled => _logger.LogInformation("Project can be saved: {Enabled}", enabled))
            .DisposeWith(disposables);
    }

    private void WireValidationTracking(CompositeDisposable disposables)
    {
        Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => CurrentValidationSummary.CollectionChanged += handler,
                handler => CurrentValidationSummary.CollectionChanged -= handler)
            .Select(_ => CurrentValidationSummary.Count > 0)
            .StartWith(CurrentValidationSummary.Count > 0)
            .ToProperty(this, vm => vm.HasValidationSummaryItems, out _hasValidationSummaryItems)
            .DisposeWith(disposables);
    }

    private void WireNavigation(CompositeDisposable disposables)
    {
        this.WhenAnyValue(vm => vm.SelectedNavigationItem)
            .Where(navItem => navItem is not null)
            .Subscribe(navItem => NavigateToPage(navItem!))
            .DisposeWith(disposables);
    }

    private async Task OpenProjectAsync()
    {
        if (_store.IsDirty)
        {
            var action = await PromptDiscardAsync();

            if (action == DiscardAction.Cancel)
            {
                return;
            }

            if (action == DiscardAction.Save)
            {
                await SaveAsync();
            }
        }

        var filePath = await OpenFileInteraction.Handle(StudioFilesFilter);

        if (filePath is null)
        {
            return;
        }

        await _store.OpenAsync(filePath);

        // Navigate to the Project page — triggers SelectedNavigationItem subscription.
        SelectNavigationItem<ProjectViewModel>();
    }

    private async Task NewProjectAsync()
    {
        if (_store.IsDirty)
        {
            var action = await PromptDiscardAsync();

            if (action == DiscardAction.Cancel)
            {
                return;
            }

            if (action == DiscardAction.Save)
            {
                await SaveAsync();
            }
        }

        var document = _projectService.CreateFromDefaults();

        var filePath = await SaveFileInteraction.Handle(StudioFilesFilter);

        if (filePath is null)
        {
            return;
        }

        await _projectService.SaveAsync(document, filePath);

        await _store.OpenAsync(filePath);

        _logger.LogInformation("New project created: {FilePath}", filePath);

        SelectNavigationItem<ProjectViewModel>();
    }

    private async Task NewFromExistingAsync()
    {
        if (_store.IsDirty)
        {
            var action = await PromptDiscardAsync();

            if (action == DiscardAction.Cancel)
            {
                return;
            }

            if (action == DiscardAction.Save)
            {
                await SaveAsync();
            }
        }

        var sourcePath = await OpenFileInteraction.Handle(StudioFilesFilter);

        if (sourcePath is null)
        {
            return;
        }

        var document = await _projectService.OpenAsync(sourcePath);

        var destinationPath = await SaveFileInteraction.Handle(StudioFilesFilter);

        if (destinationPath is null)
        {
            return;
        }

        await _projectService.SaveAsync(document, destinationPath);

        await _store.OpenAsync(destinationPath);

        _logger.LogInformation("New project created from existing: {SourcePath} → {DestinationPath}",
            sourcePath, destinationPath);

        SelectNavigationItem<ProjectViewModel>();
    }

    private async Task SaveAsync()
    {
        await _store.SaveAsync();
    }

    private async Task SaveAsAsync()
    {
        var filePath = await SaveFileInteraction.Handle(StudioFilesFilter);

        if (filePath is null)
        {
            return;
        }

        await _store.SaveAsAsync(filePath);
    }

    private async Task CloseProjectAsync()
    {
        if (_store.IsDirty)
        {
            var action = await PromptDiscardAsync();

            if (action == DiscardAction.Cancel)
            {
                return;
            }

            if (action == DiscardAction.Save)
            {
                await SaveAsync();
            }
        }

        _store.Close();

        // Clear the workspace and deselect navigation so the next open triggers the nav subscription.
        CurrentPage = null;
        SelectedNavigationItem = null;
    }

    private ReactiveCommand<Unit, Unit> CreateSaveCommand()
    {
        var canSave = _store.WhenAnyValue(store => store.IsDirty);

        return ReactiveCommand.CreateFromTask(SaveAsync, canSave);
    }

    private ReactiveCommand<Unit, Unit> CreateSaveAsCommand()
    {
        var canSaveAs = _store.WhenAnyValue(store => store.HasDocument);

        return ReactiveCommand.CreateFromTask(SaveAsAsync, canSaveAs);
    }

    private ReactiveCommand<Unit, Unit> CreateCloseProjectCommand()
    {
        var canClose = _store.WhenAnyValue(store => store.HasDocument);

        return ReactiveCommand.CreateFromTask(CloseProjectAsync, canClose);
    }

    /// <summary>Prompts the user to save or discard changes.</summary>
    public async Task<DiscardAction> PromptDiscardAsync()
    {
        var projectName = _store.MetadataEditor.ProjectName.Value;

        var displayName = projectName.IsNotNullOrEmpty()
            ? projectName
            : "Untitled";

        return await ConfirmDiscardInteraction.Handle(displayName);
    }

    /// <summary>Selects the navigation item whose <see cref="NavigationItemViewModel.ViewModelType"/>
    /// matches <typeparamref name="TViewModel"/>, which triggers the nav subscription to show the
    /// corresponding page view via <see cref="NavigateToPage"/>.</summary>
    /// <typeparam name="TViewModel">The page view model type to select.</typeparam>
    private void SelectNavigationItem<TViewModel>()
    {
        SelectedNavigationItem = NavigationItems.Single(item => item.ViewModelType == typeof(TViewModel));

        _logger.LogInformation("Selected navigation item: {PageName}", SelectedNavigationItem.DisplayName);
    }

    /// <summary>Navigates to the workspace page corresponding to the selected navigation item.</summary>
    private void NavigateToPage(NavigationItemViewModel viewModel)
    {
        _logger.LogInformation("Navigating to page: {PageName}", viewModel.DisplayName);

        CurrentPage = viewModel.CreateView(_viewFactory);
    }
}
