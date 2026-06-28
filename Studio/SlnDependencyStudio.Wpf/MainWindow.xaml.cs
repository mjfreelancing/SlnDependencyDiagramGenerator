using AllOverIt.Assertion;
using AllOverIt.ReactiveUI.Factories;
using Microsoft.Win32;
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
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly IViewFactory _viewFactory;
    private readonly IApplicationSettingsService _settingsService;

    public MainWindow(MainWindowViewModel vieModel, IViewFactory viewFactory, IApplicationSettingsService settingsService)
    {
        _viewFactory = viewFactory.WhenNotNull();
        _settingsService = settingsService.WhenNotNull();

        ViewModel = vieModel;
        DataContext = vieModel;

        InitializeComponent();

        RestorePlacement(_settingsService.CurrentState.WindowPlacement);

        this.WhenActivated(disposables =>
        {
            // Open the settings dialog when the Settings menu item is clicked.
            this.BindCommand(ViewModel, vm => vm.OpenSettingsCommand, view => view.SettingsMenuItem)
                .DisposeWith(disposables);

            ViewModel!
                .OpenSettingsCommand
                .Subscribe(_ => OpenSettingsDialog())
                .DisposeWith(disposables);

            // Open Project menu item (also triggered by Ctrl+O).
            this.BindCommand(ViewModel, vm => vm.OpenProjectCommand, view => view.OpenProjectMenuItem)
                .DisposeWith(disposables);

            // Open-file dialog interaction.
            ViewModel!
                .OpenFileInteraction
                .RegisterHandler(context =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = "Open Dependency Project",
                        Filter = context.Input,
                        CheckFileExists = true
                    };

                    var output = dialog.ShowDialog() == true ? dialog.FileName : null;

                    context.SetOutput(output);
                })
                .DisposeWith(disposables);

            // Exit menu item.
            this.BindCommand(ViewModel, vm => vm.ExitCommand, view => view.ExitMenuItem)
                .DisposeWith(disposables);

            ViewModel!
                .ExitCommand
                .Subscribe(_ => Close())
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
