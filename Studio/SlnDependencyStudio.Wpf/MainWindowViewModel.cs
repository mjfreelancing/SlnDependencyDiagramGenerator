using AllOverIt.Extensions;   // IsNotNullOrEmpty
using AllOverIt.ReactiveUI;
using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using SlnDependencyStudio.Wpf.Features.Run;
using SlnDependencyStudio.Wpf.Features.Solution;
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

    private readonly IProjectDocumentStore _store;
    private readonly IDependencyProjectService _projectService;
    private readonly IRecentProjectsService _recentProjectsService;
    private readonly IErrorDialogService _errorDialog;
    private readonly IViewFactory _viewFactory;
    private readonly IToolStatusService _toolStatus;
    private readonly IPreGenerationAnalysisService _analysisService;
    private readonly IGenerationService _generationService;
    private readonly OutputPanelViewModel _outputPanelViewModel;
    private readonly ILogger<MainWindowViewModel> _logger;
    private CancellationTokenSource? _operationCts;

    // OAPHs initialized here with their real observable sources so they are
    // never null. They live for the lifetime of the ViewModel.
    private readonly ObservableAsPropertyHelper<bool> _hasDocument;
    private readonly ObservableAsPropertyHelper<bool> _hasRecentProjects;
    private bool _canClose = true;
    private bool _isOperationRunning;
    private bool _runMenuEnabled;
    private NavigationItemViewModel? _selectedNavigationItem;
    private object? _currentPage;

    // Tracks per-page validation subscriptions so old subscriptions are cleaned
    // up when the user navigates to a different page.
    private readonly CompositeDisposable _pageValidationSubscriptions = [];

    /// <summary>The navigation items displayed in the left sidebar.</summary>
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    /// <summary>The currently selected navigation item.</summary>
    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set => this.RaiseAndSetIfChanged(ref _selectedNavigationItem, value);
    }

    /// <summary>The view for the current centre workspace page.</summary>
    public object? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>Validation errors collected across all editable sections.</summary>
    public ObservableCollection<ValidationSummaryItem> CurrentValidationSummary { get; } = [];

    /// <summary>Whether the main window can be closed. False when a command is executing.</summary>
    public bool CanClose
    {
        get => _canClose;
        private set => this.RaiseAndSetIfChanged(ref _canClose, value);
    }

    /// <summary>
    /// <see langword="true"/> when a long-running operation (analyse or generate)
    /// is in progress. Bound by a <c>Menu.Resources</c> style to disable all
    /// <see cref="MenuItem"/>s during execution — new menus added anywhere in
    /// the <c>Menu</c> inherit this behaviour automatically.
    /// </summary>
    public bool IsOperationRunning
    {
        get => _isOperationRunning;
        private set => this.RaiseAndSetIfChanged(ref _isOperationRunning, value);
    }

    /// <summary><see langword="true"/> when a document is currently loaded in the store.</summary>
    public bool HasDocument => _hasDocument.Value;

    /// <summary>Whether the Run menu is enabled.</summary>
    public bool RunMenuEnabled
    {
        get => _runMenuEnabled;
        private set => this.RaiseAndSetIfChanged(ref _runMenuEnabled, value);
    }

    /// <summary>The output panel view (bottom of the window).</summary>
    public object OutputPanel { get; }

    /// <summary>Command that runs a dry-run analysis.</summary>
    public ReactiveCommand<Unit, Unit> AnalyzeCommand { get; }

    /// <summary>Command that runs generation (Phase 8 placeholder).</summary>
    public ReactiveCommand<Unit, Unit> GenerateCommand { get; }

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

    /// <summary>Recently opened project files, most recent first.</summary>
    public ObservableCollection<RecentProjectEntry> RecentProjects { get; } = [];

    /// <summary><see langword="true"/> when at least one recent project exists.</summary>
    public bool HasRecentProjects => _hasRecentProjects.Value;

    /// <summary>Command that opens a recent project from the list.</summary>
    public ReactiveCommand<string, Unit> OpenRecentProjectCommand { get; }

    /// <summary>Command that closes the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel(IProjectDocumentStore store, IDependencyProjectService projectService,
        IRecentProjectsService recentProjectsService, IErrorDialogService errorDialog, IViewFactory viewFactory,
        IToolStatusService toolStatus, IPreGenerationAnalysisService analysisService,
        IGenerationService generationService, ILogger<MainWindowViewModel> logger)
    {
        _store = store;
        _projectService = projectService;
        _recentProjectsService = recentProjectsService;
        _errorDialog = errorDialog;
        _viewFactory = viewFactory;
        _toolStatus = toolStatus;
        _analysisService = analysisService;
        _generationService = generationService;
        _logger = logger;

        var outputPanelView = _viewFactory.CreateViewFor<OutputPanelViewModel>();
        OutputPanel = outputPanelView;
        _outputPanelViewModel = (OutputPanelViewModel)outputPanelView.ViewModel!;

        // OAPHs initialized here with their real observable sources so they are
        // never null. They live for the lifetime of the ViewModel.
        RefreshRecentProjects();

        _hasDocument = _store
            .WhenAnyValue(store => store.HasDocument)
            .ToProperty(this, nameof(HasDocument));

        _hasRecentProjects = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => RecentProjects.CollectionChanged += handler,
                handler => RecentProjects.CollectionChanged -= handler)
            .Select(_ => RecentProjects.Count > 0)
            .StartWith(RecentProjects.Count > 0)
            .ToProperty(this, nameof(HasRecentProjects));

        OpenSettingsCommand = ReactiveCommand.Create(() => { });
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectAsync);
        NewFromExistingCommand = ReactiveCommand.CreateFromTask(NewFromExistingAsync);
        SaveCommand = CreateSaveCommand();
        SaveAsCommand = CreateSaveAsCommand();
        CloseProjectCommand = CreateCloseProjectCommand();
        ExitCommand = ReactiveCommand.Create(() => { });
        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentProjectAsync);

        // Mutual exclusion between Analyze and Generate is handled by RunMenuEnabled
        // which disables the entire Run menu during either operation.
        GenerateCommand = CreateGenerateCommand();
        AnalyzeCommand = CreateAnalyzeCommand();

        // Populate navigation items. Each page VM receives the store via DI and self-initialises.
        NavigationItems =
        [
            new NavigationItemViewModel<ProjectViewModel>
            {
                DisplayName = "Project",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.FileDocumentOutline
            },
            new NavigationItemViewModel<SolutionViewModel>
            {
                DisplayName = "Solution",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.FolderOpenOutline
            },
            new NavigationItemViewModel<ExportViewModel>
            {
                DisplayName = "Export",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.ExportVariant
            },
            new NavigationItemViewModel<DiagramsViewModel>
            {
                DisplayName = "Diagrams",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.GraphOutline
            },
            new NavigationItemViewModel<PipelineViewModel>
            {
                DisplayName = "Pipeline",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.Pipe
            }
        ];

        // Defer: the HasDocument subscription in OnActivated fires immediately
        // with HasDocument=false and calls ShowEmptyState() — but only after the
        // window is activated and the visual tree is ready for Loaded events.
    }

    /// <inheritdoc />
    protected override void OnActivated(CompositeDisposable disposables)
    {
        WireDocumentStateTracking(disposables);
        WireNavigation(disposables);
        WireRecentProjects(disposables);
        WireCancelCommand(disposables);
    }

    private void WireCancelCommand(CompositeDisposable disposables)
    {
        _outputPanelViewModel
            .CancelCommand
            .Subscribe(_ => _operationCts?.Cancel())
            .DisposeWith(disposables);
    }

    private void WireDocumentStateTracking(CompositeDisposable disposables)
    {
        _store
            .WhenAnyValue(store => store.HasDocument)
            .Subscribe(hasDocument =>
            {
                if (hasDocument)
                {
                    SelectNavigationItem<ProjectViewModel>();
                }
                else
                {
                    ClearNavigationValidationDots();
                    ShowEmptyState();
                }
            })
            .DisposeWith(disposables);

        _store
            .WhenAnyValue(store => store.DocumentFilePath)
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
            .Where(_ => !_store.IsTransitioning)
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

        WireRunMenuGating(disposables);
    }

    private void WireRunMenuGating(CompositeDisposable disposables)
    {
        var validationErrorsChanged = Observable
            .Merge(NavigationItems.Select(item =>
                item.WhenAnyValue(nav => nav.HasValidationError)))
            .StartWith(false);

        // Single source of truth for operation state — both Run menu and
        // the global MenuItem style in the view consume this.
        Observable
            .CombineLatest(
                AnalyzeCommand.IsExecuting,
                GenerateCommand.IsExecuting,
                (analyzing, generating) => analyzing || generating)
            .Subscribe(running => IsOperationRunning = running)
            .DisposeWith(disposables);

        Observable
            .CombineLatest(
                _store.WhenAnyValue(store => store.HasDocument),
                validationErrorsChanged,
                this.WhenAnyValue(vm => vm.IsOperationRunning),
                (hasDoc, _, running) => hasDoc && !running && !AnyValidationErrors())
            .Subscribe(enabled => RunMenuEnabled = enabled)
            .DisposeWith(disposables);

        this.WhenAnyValue(vm => vm.IsOperationRunning)
            .Subscribe(running => CanClose = !running)
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
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
        }

        var filePath = await OpenFileInteraction.Handle(StudioFilesFilter);

        if (filePath is null)
        {
            return;
        }

        try
        {
            await _store.OpenAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open project: {FilePath}", filePath);

            await _errorDialog.ShowError.Handle(
                new ErrorInfo("Open Failed", $"Could not open the project file.\n\n{ex.Message}"));

            return;
        }

        // Navigate to the Project page — triggers SelectedNavigationItem subscription.
        SelectNavigationItem<ProjectViewModel>();
    }

    private async Task NewProjectAsync()
    {
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
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
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
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
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
        }

        _store.Close();

        // The HasDocument subscription in WireDocumentStateTracking automatically
        // calls ShowEmptyState() when HasDocument becomes false. No need to
        // manually clear CurrentPage/SelectedNavigationItem here.
    }

    private ReactiveCommand<Unit, Unit> CreateGenerateCommand()
    {
        var canGenerate = _store.WhenAnyValue(store => store.HasDocument);

        return ReactiveCommand.CreateFromTask(GenerateAsync, canGenerate);
    }

    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        if (!await SaveIfDirtyAsync(cancellationToken))
        {
            return;
        }

        // Run generation via IGenerationService.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _operationCts = linkedCts;
        _outputPanelViewModel.IsOperationRunning = true;

        try
        {
            var tcs = new TaskCompletionSource();

            _generationService
                .RunAsync(linkedCts.Token)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(
                    onNext: message => _outputPanelViewModel.Messages.Add(message),
                    onError: ex => tcs.TrySetException(ex),
                    onCompleted: () => tcs.TrySetResult());

            await tcs.Task;
        }
        finally
        {
            _outputPanelViewModel.IsOperationRunning = false;
            _operationCts = null;
        }
    }

    private ReactiveCommand<Unit, Unit> CreateAnalyzeCommand()
    {
        var canAnalyze = _store.WhenAnyValue(store => store.HasDocument);

        return ReactiveCommand.CreateFromTask(AnalyzeAsync, canAnalyze);
    }

    private async Task AnalyzeAsync(CancellationToken cancellationToken)
    {
        if (!await SaveIfDirtyAsync(cancellationToken))
        {
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _operationCts = linkedCts;
        _outputPanelViewModel.IsOperationRunning = true;

        try
        {
            var tcs = new TaskCompletionSource();

            _analysisService
                .RunAsync(linkedCts.Token)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(
                    onNext: message => _outputPanelViewModel.Messages.Add(message),
                    onError: ex => tcs.TrySetException(ex),
                    onCompleted: () => tcs.TrySetResult());

            await tcs.Task;
        }
        finally
        {
            _outputPanelViewModel.IsOperationRunning = false;
            _operationCts = null;
        }
    }

    private bool AnyValidationErrors()
    {
        return NavigationItems.Any(item => item.HasValidationError);
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

    /// <summary>
    /// When the document is dirty, prompts the user to save or discard changes.
    /// Returns <see langword="false"/> if the user cancelled, <see langword="true"/> to proceed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <remarks>Selecting <see cref="DiscardAction.Discard"/> returns true so the caller continues.</remarks>
    private async Task<bool> SaveIfDirtyAsync(CancellationToken cancellationToken)
    {
        if (!_store.IsDirty)
        {
            return true;
        }

        var action = await PromptDiscardAsync();

        if (action == DiscardAction.Save)
        {
            await _store.SaveAsync(cancellationToken);
        }

        return action != DiscardAction.Cancel;
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

        var view = viewModel.CreateView(_viewFactory);
        CurrentPage = view;

        WirePageValidation(viewModel, view);
    }

    /// <summary>Wires the nav item's <c>HasValidationError</c> to the page ViewModel's
    /// <c>ValidationContext.IsValid</c> so the red dot indicator reflects real-time
    /// validation state.</summary>
    private void WirePageValidation(NavigationItemViewModel navItem, IViewFor view)
    {
        _pageValidationSubscriptions.Clear();

        if (view.ViewModel is IValidatableViewModel validatable)
        {
            validatable.ValidationContext
                .WhenAnyValue(context => context.IsValid)
                .Subscribe(isValid => navItem.HasValidationError = !isValid)
                .DisposeWith(_pageValidationSubscriptions);
        }
    }

    private void ClearNavigationValidationDots()
    {
        _pageValidationSubscriptions.Clear();

        foreach (var navItem in NavigationItems)
        {
            navItem.HasValidationError = false;
        }
    }

    /// <summary>Shows the empty-state landing page in the centre workspace, delegating
    /// all action commands from the shell so the empty-state cards trigger the same
    /// project lifecycle operations as the menu bar.</summary>
    internal void ShowEmptyState()
    {
        // Clear the selected nav item so the next SelectNavigationItem call
        // triggers a genuine WhenAnyValue change. Without this, if the user
        // opens a project, closes it, then opens another, SelectedNavigationItem
        // stays "Project" and the navigation subscription never fires.
        SelectedNavigationItem = null;

        var view = _viewFactory.CreateViewFor<EmptyStateViewModel>();
        var viewModel = view.ViewModel!;

        // Wire shell commands to the empty-state's interactions.
        viewModel.NewProjectRequested.RegisterHandler(async ctx =>
        {
            await NewProjectCommand.Execute();
            ctx.SetOutput(Unit.Default);
        });

        viewModel.OpenProjectRequested.RegisterHandler(async ctx =>
        {
            await OpenProjectCommand.Execute();
            ctx.SetOutput(Unit.Default);
        });

        viewModel.OpenRecentProjectRequested.RegisterHandler(async ctx =>
        {
            await OpenRecentProjectCommand.Execute(ctx.Input);
            ctx.SetOutput(Unit.Default);
        });

        CurrentPage = view;

        _logger.LogInformation("Showing empty-state landing page");
    }

    private void WireRecentProjects(CompositeDisposable disposables)
    {
        RefreshRecentProjects();
    }

    internal void RefreshRecentProjects()
    {
        RecentProjects.Clear();

        foreach (var entry in _recentProjectsService.GetRecent())
        {
            RecentProjects.Add(entry);
        }
    }

    private async Task OpenRecentProjectAsync(string filePath)
    {
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
        }

        try
        {
            await _store.OpenAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open recent project, removing from list: {FilePath}", filePath);

            _recentProjectsService.Remove(filePath);
            RefreshRecentProjects();

            await _errorDialog.ShowError.Handle(
                new ErrorInfo("Open Failed", $"The recent project could not be opened. It may have been moved or deleted.\n\n{ex.Message}"));

            return;
        }

        _logger.LogInformation("Opened recent project: {FilePath}", filePath);

        SelectNavigationItem<ProjectViewModel>();
    }
}
