using ReactiveUI;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

/// <summary>
/// Main application shell window. Inherits <see cref="ReactiveWindow{T}"/> from ReactiveUI.WPF
/// for automatic ViewModel activation, <c>WhenActivated</c>, and <c>BindCommand</c> support.
/// Material Design theming is applied via resource dictionaries in <c>App.xaml</c>.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// The ViewModel is created here, matching the ReactiveUI pattern from
    /// AllOverIt Demos/AllOverIt.ReactiveUI/CountdownTimerAppDemo/Views/MainWindow.xaml.cs.
    /// </summary>
    public MainWindow()
    {
        ViewModel = new MainWindowViewModel();

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Track generation state for UI gating (Phase 8).
            ViewModel!
                .WhenAnyValue(vm => vm.IsGenerating)
                .Subscribe(isGenerating =>
                {
                    // TODO Phase 8: disable editing controls, prevent close, show progress.
                })
                .DisposeWith(disposables);
        });
    }

    /// <summary>Prevents the window from closing when CanClose is <see langword="false"/> (FR-8.8).</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (ViewModel is not null && !ViewModel.CanClose)
        {
            e.Cancel = true;
        }
    }
}
