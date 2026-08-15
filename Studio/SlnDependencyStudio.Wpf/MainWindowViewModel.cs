using AllOverIt.Extensions;
using AllOverIt.ReactiveUI;
using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Enumerations;
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
using SlnDependencyStudio.Wpf.Utils;
using SlnDependencyStudio.Wpf.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf;

/// <summary>View model for the main application shell window.</summary>
public sealed class MainWindowViewModel : ActivatableViewModel, IDisposable
{
    const string StudioFilesFilter = "Studio Project files (*.sds)|*.sds|All files (*.*)|*.*";

    private readonly IProjectDocumentStore _store;
    private readonly IDependencyProjectService _projectService;
    private readonly IRecentProjectsStore _recentProjectsStore;
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
    private readonly ReactiveCommand<Unit, Unit> _cancelOperationCommand;
    private bool _canClose = true;
    private bool _isOperationRunning;
    private bool _canCancel = true;
    private string _operationName = string.Empty;
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

    /// <summary>
    /// <see langword="true"/> when the Cancel button in the operation overlay
    /// should be enabled. Flips to <see langword="false"/> as soon as the user
    /// clicks Cancel.
    /// </summary>
    public bool CanCancel
    {
        get => _canCancel;
        private set => this.RaiseAndSetIfChanged(ref _canCancel, value);
    }

    /// <summary>The display name of the currently running operation (e.g. "Operation in progress" or "Cancelling...").</summary>
    public string OperationName
    {
        get => _operationName;
        private set => this.RaiseAndSetIfChanged(ref _operationName, value);
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
    public ReactiveCommand<Unit, Unit> AnalyseCommand { get; }

    /// <summary>Command that runs generation.</summary>
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

    /// <summary>Interaction that asks the view to prompt how document-relative paths should be handled when
    /// saving the project to a different folder.</summary>
    public Interaction<RelativePathChangeInfo, SaveAsRelativePathAction> RelativePathSaveAsInteraction { get; } = new();

    /// <summary>Recently opened project files, most recent first. Shared collection from the store.</summary>
    public ObservableCollection<RecentProjectEntry> RecentProjects => _recentProjectsStore.RecentProjects;

    /// <summary><see langword="true"/> when at least one recent project exists.</summary>
    public bool HasRecentProjects => _hasRecentProjects.Value;

    /// <summary>Command that opens a recent project from the list.</summary>
    public ReactiveCommand<string, Unit> OpenRecentProjectCommand { get; }

    /// <summary>Command that removes a recent project entry from the list.</summary>
    public ReactiveCommand<string, Unit> RemoveRecentProjectCommand { get; }

    /// <summary>Command that closes the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel(IProjectDocumentStore store, IDependencyProjectService projectService,
        IRecentProjectsStore recentProjectsStore, IErrorDialogService errorDialog, IViewFactory viewFactory,
        IToolStatusService toolStatus, IPreGenerationAnalysisService analysisService,
        IGenerationService generationService, ILogger<MainWindowViewModel> logger)
    {
        _store = store;
        _projectService = projectService;
        _recentProjectsStore = recentProjectsStore;
        _errorDialog = errorDialog;
        _viewFactory = viewFactory;
        _toolStatus = toolStatus;
        _analysisService = analysisService;
        _generationService = generationService;
        _logger = logger;

        var outputPanelView = _viewFactory.CreateViewFor<OutputPanelViewModel>();
        OutputPanel = outputPanelView;
        _outputPanelViewModel = (OutputPanelViewModel)outputPanelView.ViewModel!;

        _hasDocument = _store
            .WhenAnyValue(store => store.HasDocument)
            .ToProperty(this, nameof(HasDocument));

        _hasRecentProjects = _recentProjectsStore
            .WhenAnyValue(store => store.HasRecentProjects)
            .ToProperty(this, nameof(HasRecentProjects));

        OpenSettingsCommand = ReactiveCommand.Create(() => { });
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);
        NewProjectCommand = ReactiveCommand.CreateFromTask(NewProjectAsync);
        NewFromExistingCommand = ReactiveCommand.CreateFromTask(NewFromExistingAsync);
        SaveCommand = CreateSaveCommand();
        SaveAsCommand = CreateSaveAsCommand();
        CloseProjectCommand = CreateCloseProjectCommand();
        ExitCommand = ReactiveCommand.Create(() => { });
        _cancelOperationCommand = CreateCancelOperationCommand();
        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentProjectAsync);
        RemoveRecentProjectCommand = ReactiveCommand.Create<string>(RemoveRecentProject);

        // Mutual exclusion between Analyse and Generate is handled by RunMenuEnabled
        // which disables the entire Run menu during either operation.
        GenerateCommand = CreateGenerateCommand();
        AnalyseCommand = CreateAnalyseCommand();

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
            new NavigationItemViewModel<DiagramsViewModel>
            {
                DisplayName = "Diagrams",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.GraphOutline
            },
            new NavigationItemViewModel<ExportViewModel>
            {
                DisplayName = "Export",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.ExportVariant
            },
            new NavigationItemViewModel<PipelineViewModel>
            {
                DisplayName = "Pipeline",
                IconKind = MaterialDesignThemes.Wpf.PackIconKind.Pipe
            }
        ];

        // Defer: the DocumentEpoch subscription in OnActivated fires immediately
        // (DocumentEpoch=0, HasDocument=false) and calls ShowEmptyState() — but only
        // after the window is activated and the visual tree is ready for Loaded events.
    }

    /// <inheritdoc />
    protected override void OnActivated(CompositeDisposable disposables)
    {
        WireDocumentStateTracking(disposables);
        WireNavigation(disposables);
        WireCancelCommand(disposables);
        WireNavigationDirtyState(disposables);
    }

    private void WireCancelCommand(CompositeDisposable disposables)
    {
        // Output panel's own Cancel button — delegates to the cancel command
        // which is gated by CanCancel so double-clicks are ignored.
        _outputPanelViewModel
            .CancelCommand
            .Where(_ => CanCancel)
            .Subscribe(_ => _cancelOperationCommand.Execute().Subscribe())
            .DisposeWith(disposables);
    }

    private ReactiveCommand<Unit, Unit> CreateCancelOperationCommand()
    {
        var canCancel = this.WhenAnyValue(vm => vm.CanCancel);

        return ReactiveCommand.CreateFromTask(CancelOperationAsync, canCancel);
    }

    private async Task CancelOperationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancellation requested");

        _operationCts?.Cancel();

        CanCancel = false;
        _outputPanelViewModel.CanCancel = false;
        OperationName = "Cancelling...";

        // Yield one dispatcher tick so that ReactiveCommand's CanExecute
        // (gated by CanCancel) propagates to the button's IsEnabled before
        // the command completes — the button visually disables immediately.
        await Task.Yield();
    }

    private void WireDocumentStateTracking(CompositeDisposable disposables)
    {
        // Single source of truth for page selection. The store's DocumentEpoch counter
        // changes on every successful open (including replacing an already-open document)
        // and on close, but never on save/save-as — so commands perform their operation
        // only, and navigation is derived here. See IProjectDocumentStore.DocumentEpoch.
        _store
            .WhenAnyValue(store => store.DocumentEpoch)
            .Subscribe(_ =>
            {
                if (_store.HasDocument)
                {
                    // Re-opening a document invalidates validation state captured from page
                    // instances of the previous document (page VMs are created on demand and
                    // disposed on navigation). Reset the dots so stale indicators don't linger
                    // after re-opening; the current page's indicator is re-established by the
                    // navigation below via WirePageValidation, or re-evaluated by its still-active
                    // subscription when staying on the same page.
                    ResetNavigationValidationDots();

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
                    _logger.LogDebug("No project is currently loaded");
                }
                else
                {
                    _logger.LogDebug("Current project: {FilePath}", filePath);
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
                    _logger.LogDebug("Current project has unsaved changes");
                }
                else
                {
                    _logger.LogDebug("Current project is clean (no unsaved changes)");
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
                AnalyseCommand.IsExecuting,
                GenerateCommand.IsExecuting,
                (analysing, generating) => analysing || generating)
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

    /// <summary>Wires each nav item's dirty indicator to the store editors that back its page,
    /// so the sidebar reflects unsaved changes per section rather than for the whole document.</summary>
    private void WireNavigationDirtyState(CompositeDisposable disposables)
    {
        // Each page delegates editing to a specific set of store editors; the dirty
        // indicator for a nav item reflects only the section it represents.
        var dirtySources = new Dictionary<Type, IObservable<bool>>
        {
            [typeof(ProjectViewModel)] = _store.MetadataEditor.WhenAnyValue(editor => editor.IsDirty),
            [typeof(SolutionViewModel)] = _store.SolutionOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
            [typeof(ExportViewModel)] = _store.ExportOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
            [typeof(DiagramsViewModel)] = _store.DiagramOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
            [typeof(PipelineViewModel)] = Observable.CombineLatest(
                _store.PreGenerationEditor.WhenAnyValue(editor => editor.IsDirty),
                _store.RestoreSolutionEditor.WhenAnyValue(editor => editor.IsDirty),
                _store.PostGenerationEditor.WhenAnyValue(editor => editor.IsDirty),
                (preGen, restore, postGen) => preGen || restore || postGen)
        };

        foreach (var item in NavigationItems)
        {
            if (dirtySources.TryGetValue(item.ViewModelType, out var source))
            {
                // The editors pass through transient dirty states while a document is opening or
                // closing (IsTransitioning). Gate the indicator on that flag so a freshly opened
                // project always starts clean; genuine edits made after the transition still
                // surface normally.
                source
                    .WithLatestFrom(
                        _store.WhenAnyValue(store => store.IsTransitioning),
                        (isDirty, isTransitioning) => isTransitioning ? false : isDirty)
                    .Subscribe(isDirty => item.HasUnsavedChanges = isDirty)
                    .DisposeWith(disposables);
            }
        }
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

        var sourceDirectory = Path.GetDirectoryName(sourcePath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        var affectedFields = RelativePathRebaser.GetRelativePathFields(document);

        if (ShouldPromptForRelativePaths(sourceDirectory, destinationDirectory, affectedFields))
        {
            var action = await RelativePathSaveAsInteraction.Handle(new RelativePathChangeInfo(affectedFields));

            if (action == SaveAsRelativePathAction.Cancel)
            {
                return;
            }

            RebaseDocumentPaths(document, sourceDirectory ?? string.Empty, destinationDirectory ?? string.Empty, action);
        }

        await _projectService.SaveAsync(document, destinationPath);

        await _store.OpenAsync(destinationPath);

        _logger.LogInformation("New project created from existing: {SourcePath} → {DestinationPath}",
            sourcePath, destinationPath);
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

        var affectedFields = _store.GetRelativePathFields();

        if (ShouldPromptForRelativePaths(_store.DocumentDirectory, Path.GetDirectoryName(filePath), affectedFields))
        {
            var action = await RelativePathSaveAsInteraction.Handle(new RelativePathChangeInfo(affectedFields));

            if (action == SaveAsRelativePathAction.Cancel)
            {
                return;
            }

            await _store.SaveAsAsync(filePath, action);
        }
        else
        {
            await _store.SaveAsAsync(filePath);
        }
    }

    /// <summary>Whether the user should be prompted about relative paths when saving to <paramref name="newDirectory"/>.
    /// Prompts only when at least one relative path would change meaning (i.e. the folder actually differs).</summary>
    private static bool ShouldPromptForRelativePaths(string? oldDirectory, string? newDirectory, IReadOnlyList<RelativePathField> affectedFields)
    {
        return affectedFields.Count > 0
            && oldDirectory.IsNotNullOrEmpty()
            && newDirectory.IsNotNullOrEmpty()
            && !PathEqualityComparer.Default.Equals(oldDirectory, newDirectory);
    }

    /// <summary>Re-writes the document's relative paths so they keep pointing at the same target after the document
    /// moves from <paramref name="oldDirectory"/> to <paramref name="newDirectory"/>.</summary>
    private static void RebaseDocumentPaths(DependencyProjectDocument document, string oldDirectory, string newDirectory, SaveAsRelativePathAction action)
    {
        document.DiagramGenerator.Solution.SolutionPath = RelativePathRebaser.Rewrite(document.DiagramGenerator.Solution.SolutionPath, oldDirectory, newDirectory, action);
        document.DiagramGenerator.Export.RootPath = RelativePathRebaser.Rewrite(document.DiagramGenerator.Export.RootPath, oldDirectory, newDirectory, action);
        document.PreGeneration.WorkingDirectory = RelativePathRebaser.Rewrite(document.PreGeneration.WorkingDirectory, oldDirectory, newDirectory, action);
        document.PostGeneration.WorkingDirectory = RelativePathRebaser.Rewrite(document.PostGeneration.WorkingDirectory, oldDirectory, newDirectory, action);
    }

    private async Task CloseProjectAsync()
    {
        if (!await SaveIfDirtyAsync(CancellationToken.None))
        {
            return;
        }

        _store.Close();

        // The DocumentEpoch subscription in WireDocumentStateTracking automatically
        // calls ShowEmptyState() when the document is closed. No need to
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

        _logger.LogInformation("Generate started");

        // Run generation via IGenerationService.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _operationCts = linkedCts;
        _outputPanelViewModel.IsOperationRunning = true;
        _outputPanelViewModel.CanCancel = true;

        CanCancel = true;
        OperationName = "Operation in progress";

        try
        {
            // Output is streamed to the output panel via the shared Serilog sink.
            await _generationService.RunAsync(linkedCts.Token);
        }
        finally
        {
            _outputPanelViewModel.IsOperationRunning = false;
            _operationCts = null;
            OperationName = string.Empty;
        }
    }

    private ReactiveCommand<Unit, Unit> CreateAnalyseCommand()
    {
        var canAnalyse = _store.WhenAnyValue(store => store.HasDocument);

        return ReactiveCommand.CreateFromTask(AnalyseAsync, canAnalyse);
    }

    private async Task AnalyseAsync(CancellationToken cancellationToken)
    {
        if (!await SaveIfDirtyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Analyse started");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _operationCts = linkedCts;
        _outputPanelViewModel.IsOperationRunning = true;
        _outputPanelViewModel.CanCancel = true;

        CanCancel = true;
        OperationName = "Operation in progress";

        try
        {
            // Output is streamed to the output panel via the shared Serilog sink.
            await _analysisService.RunAsync(linkedCts.Token);
        }
        finally
        {
            _outputPanelViewModel.IsOperationRunning = false;
            _operationCts = null;
            OperationName = string.Empty;
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

        _logger.LogDebug("Selected navigation item: {PageName}", SelectedNavigationItem.DisplayName);
    }

    /// <summary>Navigates to the workspace page corresponding to the selected navigation item.</summary>
    private void NavigateToPage(NavigationItemViewModel viewModel)
    {
        _logger.LogDebug("Navigating to page: {PageName}", viewModel.DisplayName);

        if (CurrentPage is IViewFor { ViewModel: IDisposable disposableVm })
        {
            disposableVm.Dispose();
        }

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

    /// <summary>Disposes the current page's validation subscription and resets every nav item's
    /// validation indicator. Used when the document is closed and the workspace is replaced by
    /// the empty state, so no validation subscription outlives the page it was created for.</summary>
    private void ClearNavigationValidationDots()
    {
        _pageValidationSubscriptions.Clear();

        ResetNavigationValidationDots();
    }

    /// <summary>Resets the validation indicator on every nav item without disturbing the current
    /// page's validation subscription. Used when a document is (re)opened so stale dots from the
    /// previous document don't linger; the current page's indicator is either re-established by
    /// navigation or re-evaluated by its still-active subscription.</summary>
    private void ResetNavigationValidationDots()
    {
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

        _logger.LogDebug("Showing empty-state landing page");
    }

    /// <summary>Reloads the recent projects list from the store.</summary>
    internal void RefreshRecentProjects()
    {
        _recentProjectsStore.Refresh();
    }

    private async Task OpenRecentProjectAsync(string filePath)
    {
        _logger.LogInformation("Opening recent project: {FilePath}", filePath);

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

            _recentProjectsStore.Remove(filePath);

            await _errorDialog.ShowError.Handle(
                new ErrorInfo("Open Failed", $"The recent project could not be opened. It may have been moved or deleted.\n\n{ex.Message}"));

            return;
        }
    }

    private void RemoveRecentProject(string filePath)
    {
        _recentProjectsStore.Remove(filePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _hasDocument.Dispose();
        _hasRecentProjects.Dispose();
    }
}
