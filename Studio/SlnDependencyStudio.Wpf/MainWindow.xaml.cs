using AllOverIt.Assertion;
using AllOverIt.ReactiveUI.Factories;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Settings;
using System.ComponentModel;
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
    private readonly IViewFactory _viewFactory;
    private readonly IApplicationSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// The ViewModel is created here, matching the ReactiveUI pattern from
    /// AllOverIt Demos/AllOverIt.ReactiveUI/CountdownTimerAppDemo/Views/MainWindow.xaml.cs.
    /// </summary>
    public MainWindow(MainWindowViewModel vieModel, IViewFactory viewFactory, IApplicationSettingsService settingsService)
    {
        _viewFactory = viewFactory.WhenNotNull();
        _settingsService = settingsService.WhenNotNull();
        
        ViewModel = vieModel;

        InitializeComponent();

        RestorePlacement(_settingsService.CurrentState.WindowPlacement);

        this.WhenActivated(disposables =>
        {
            // Open the settings dialog when the Settings nav button is clicked.
            this.BindCommand(ViewModel, vm => vm.OpenSettingsCommand, view => view.SettingsButton)
                .DisposeWith(disposables);

            ViewModel!
                .OpenSettingsCommand
                .Subscribe(_ => OpenSettingsDialog())
                .DisposeWith(disposables);

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

    /// <summary>Saves the current window placement to application state before closing.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (ViewModel is not null && !ViewModel.CanClose)
        {
            e.Cancel = true;
            return;
        }

        var placement = new WindowPlacement
        {
            Left = RestoreBounds.Left,
            Top = RestoreBounds.Top,
            Width = RestoreBounds.Width,
            Height = RestoreBounds.Height,
            State = WindowState.ToString()
        };

        _settingsService.CurrentState.WindowPlacement = placement;
        _settingsService.SaveState();
    }

    private void OpenSettingsDialog()
    {
        var view = (Window)_viewFactory.CreateViewFor<SettingsWindowViewModel>();
        view.Owner = this;
        view.ShowDialog();
    }

    private void RestorePlacement(WindowPlacement? placement)
    {
        if (placement is null || !placement.IsOnScreen())
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = placement.Left;
        Top = placement.Top;
        Width = placement.Width;
        Height = placement.Height;

        if (placement.State is not "Normal")
        {
            Loaded += (_, _) =>
            {
                WindowState = placement.State switch
                {
                    "Maximized" => WindowState.Maximized,
                    _ => WindowState.Normal
                };
            };
        }
    }
}
