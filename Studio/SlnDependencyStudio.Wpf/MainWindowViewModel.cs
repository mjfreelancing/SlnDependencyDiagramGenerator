using AllOverIt.ReactiveUI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Wpf.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf;

/// <summary>View model for the main application shell window.
/// Created manually by <see cref="MainWindow"/> per the ReactiveUI pattern — not resolved from DI.</summary>
public sealed class MainWindowViewModel : ActivatableViewModel
{
    private ObservableAsPropertyHelper<bool> _hasValidationSummaryItems = null!;

    /// <summary>The navigation items displayed in the left sidebar. Bound to the nav <c>ListBox</c>.</summary>
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    /// <summary>The currently selected navigation item. Two-way bound to the nav <c>ListBox</c>.</summary>
    [Reactive]
    public NavigationItemViewModel? SelectedNavigationItem { get; set; }

    /// <summary>The view model for the current centre workspace page. When <see langword="null"/>,
    /// the empty-state landing page is shown.</summary>
    [Reactive]
    public object? CurrentPage { get; set; }

    /// <summary>Validation errors collected across all editable sections. Drives the floating
    /// validation summary bar above the output panel.</summary>
    public ObservableCollection<ValidationSummaryItem> CurrentValidationSummary { get; } = [];

    /// <summary>Whether the main window can be closed. Set to <see langword="false"/> during active generation
    /// to prevent accidental window close (FR-8.8).</summary>
    [Reactive]
    public bool CanClose { get; set; } = true;

    /// <summary>Whether a generation run is currently in progress.</summary>
    [Reactive]
    public bool IsGenerating { get; set; }

    /// <summary><see langword="true"/> when the validation summary bar should be visible.
    /// Derived from <see cref="CurrentValidationSummary"/> via <see cref="ObservableAsPropertyHelper{T}"/>.</summary>
    public bool HasValidationSummaryItems => _hasValidationSummaryItems.Value;

    /// <summary>Command that opens the application settings dialog.</summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

    /// <summary>Initializes a new instance of <see cref="MainWindowViewModel"/>.</summary>
    public MainWindowViewModel()
    {
        OpenSettingsCommand = ReactiveCommand.Create(() => { });
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
    }
}
