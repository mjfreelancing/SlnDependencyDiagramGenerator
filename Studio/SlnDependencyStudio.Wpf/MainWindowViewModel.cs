using AllOverIt.Assertion;
using AllOverIt.Extensions;   // IsNotNullOrEmpty
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

    /// <summary>The view for the current centre workspace page.</summary>
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

    /// <summary>Command that saves the current project via the <c>IDependencyProjectService</c>.</summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Command that saves the current project to a new file path.</summary>
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    /// <summary>Interaction for showing an open-file dialog. Returns the selected path or <see langword="null"/>.</summary>
    public Interaction<string, string?> OpenFileInteraction { get; } = new();

    /// <summary>Interaction for showing a save-file dialog. Returns the selected path or <see langword="null"/>.</summary>
    public Interaction<string, string?> SaveFileInteraction { get; } = new();

    /// <summary>Interaction for showing a save-before-discard confirmation dialog.
    /// Returns <see langword="true"/> if the user chose to save, <see langword="false"/> to discard,
    /// and <see langword="null"/> to cancel.</summary>
    public Interaction<string, bool?> ConfirmDiscardInteraction { get; } = new();

    /// <summary>Command that closes the application.</summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel(IDependencyProjectService projectService, IViewFactory viewFactory)
    {
        _projectService = projectService;
        _viewFactory = viewFactory;

        OpenSettingsCommand = ReactiveCommand.Create(() => { });

        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProjectAsync);

        // Save (Ctrl+S) — enabled only when the current project has unsaved changes.
        //
        // The canExecute observable must react to two independent events:
        //   1. CurrentProject itself changing  (null → project → different project → null)
        //   2. IsDirty changing on the SAME project (false → true → false as the user edits)
        //
        // A simple `WhenAnyValue(CurrentProject).Select(p => p?.IsDirty)` only handles case 1
        // because `WhenAnyValue` re-evaluates only when the referenced property (CurrentProject)
        // changes, not when a nested property (IsDirty) changes.
        //
        // The solution uses ReactiveUI's `Switch` combinator:
        //
        //   Outer observable         Inner observables (one per project)
        //   ─────────────────        ────────────────────────────────────
        //   CurrentProject = null ─► Observable.Return(false) → emits false
        //   CurrentProject = P1   ─► P1.WhenAnyValue(doc => doc.IsDirty) → emits P1.IsDirty changes
        //   P1.IsDirty → true     ─► … same inner, emits true → Save enabled
        //   P1.IsDirty → false    ─► … same inner, emits false → Save disabled
        //   CurrentProject = P2   ─► Switch disposes P1's inner, subscribes to P2's inner
        //   CurrentProject = null ─► Switch disposes P2's inner, subscribes to Return(false)
        //
        // `Switch` remembers the latest inner observable and auto-disposes the previous one.
        var canSaveProject =
            this.WhenAnyValue(vm => vm.CurrentProject)             // outer: fires when project ref changes
                .Select(project => project is not null
                    ? project.WhenAnyValue(doc => doc.IsDirty)     // inner: fires when IsDirty changes
                    : Observable.Return(false))                    // no project → always disabled
                .Switch();

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSaveProject);


        var canSaveAsProject = this.WhenAnyValue(vm => vm.CurrentProject).Select(project => project is not null);

        SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAsAsync, canSaveAsProject);


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

                    var projectVm = (ProjectViewModel)viewModel;

                    projectVm.LoadFrom(CurrentProject.Document);

                    // Wire per-page dirty tracking to the document VM.
                    projectVm.WhenAnyValue(vm => vm.IsDirty)
                        .BindTo(CurrentProject, doc => doc.IsProjectPageDirty);
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
        if (CurrentProject is { IsDirty: true })
        {
            var shouldSave = await PromptDiscardAsync(CurrentProject);

            if (shouldSave is null)
            {
                return; // Cancelled
            }

            if (shouldSave.Value)
            {
                await SaveAsync();
            }
        }

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

    private async Task SaveAsync()
    {
        Throw<InvalidOperationException>.WhenNull(CurrentProject, "No project is loaded");
        Throw<InvalidOperationException>.WhenNull(CurrentProject.CurrentFilePath, "Current project has no file path — use Save As");

        // Apply all page VM changes to the document before saving.
        ApplyAllPageChanges();

        await _projectService.SaveAsync(CurrentProject.Document, CurrentProject.CurrentFilePath);

        // Mark all page VMs as clean (updates IsDirty).
        MarkAllPagesClean();
    }

    private async Task SaveAsAsync()
    {
        Throw<InvalidOperationException>.WhenNull(CurrentProject, "No project is loaded");

        var filePath = await SaveFileInteraction.Handle(
            "SlnDependencyStudio project files (*.sds)|*.sds|All files (*.*)|*.*");

        if (filePath is null)
        {
            return;
        }

        // Apply all page VM changes to the document before saving.
        ApplyAllPageChanges();

        await _projectService.SaveAsync(CurrentProject.Document, filePath);

        CurrentProject.CurrentFilePath = filePath;

        // Mark all page VMs as clean.
        MarkAllPagesClean();
    }

    /// <summary>Applies changes from the currently displayed page VM to the document.
    /// Called before save operations to ensure the document reflects the latest edits.</summary>
    private void ApplyAllPageChanges()
    {
        if (CurrentPage is ProjectView projectView)
        {
            projectView.ViewModel!.ApplyToDocument();
        }
    }

    /// <summary>Marks all page VMs as clean. Called after a successful save.</summary>
    private void MarkAllPagesClean()
    {
        if (CurrentPage is ProjectView projectView)
        {
            projectView.ViewModel!.MarkClean();
        }
    }

    /// <summary>Prompts the user to save or discard changes. Returns <see langword="true"/> for save,
    /// <see langword="false"/> for discard, and <see langword="null"/> for cancel.</summary>
    public async Task<bool?> PromptDiscardAsync(DependencyProjectViewModel project)
    {
        var projectName = project.Document.Metadata.ProjectName.IsNotNullOrEmpty()
            ? project.Document.Metadata.ProjectName
            : "Untitled";

        return await ConfirmDiscardInteraction.Handle(projectName);
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
