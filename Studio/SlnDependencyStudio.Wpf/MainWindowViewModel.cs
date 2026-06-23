using AllOverIt.ReactiveUI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf;

/// <summary>View model for the main application shell window.
/// Created manually by <see cref="MainWindow"/> per the ReactiveUI pattern — not resolved from DI.</summary>
public sealed class MainWindowViewModel : ActivatableViewModel
{
    /// <summary>Whether the main window can be closed. Set to <see langword="false"/> during active generation
    /// to prevent accidental window close (FR-8.8).</summary>
    [Reactive]
    public bool CanClose { get; set; } = true;

    /// <summary>Whether a generation run is currently in progress.</summary>
    [Reactive]
    public bool IsGenerating { get; set; }

    /// <inheritdoc />
    protected override void OnActivated(CompositeDisposable disposables)
    {
    }
}
