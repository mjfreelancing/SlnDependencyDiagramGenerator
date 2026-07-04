using AllOverIt.Assertion;
using AllOverIt.ReactiveUI.Factories;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Settings;
using SlnDependencyStudio.Wpf.Models;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly IViewFactory _viewFactory;
    private readonly IApplicationSettingsService _settingsService;
    private readonly IProjectDocumentStore _store;

    public MainWindow(
        MainWindowViewModel viewModel,
        IViewFactory viewFactory,
        IApplicationSettingsService settingsService,
        IProjectDocumentStore store)
    {
        _viewFactory = viewFactory.WhenNotNull();
        _settingsService = settingsService.WhenNotNull();
        _store = store.WhenNotNull();

        ViewModel = viewModel;
        DataContext = viewModel;

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

            // Save menu item (Ctrl+S).
            this.BindCommand(ViewModel, vm => vm.SaveCommand, view => view.SaveMenuItem)
                .DisposeWith(disposables);

            // Save As menu item.
            this.BindCommand(ViewModel, vm => vm.SaveAsCommand, view => view.SaveAsMenuItem)
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

            // Save-file dialog interaction.
            ViewModel!
                .SaveFileInteraction
                .RegisterHandler(context =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Title = "Save Dependency Project As",
                        Filter = context.Input,
                        DefaultExt = ".sds",
                        AddExtension = true
                    };

                    var output = dialog.ShowDialog() == true ? dialog.FileName : null;

                    context.SetOutput(output);
                })
                .DisposeWith(disposables);

            // Save-before-discard confirmation dialog.
            ViewModel!
                .ConfirmDiscardInteraction
                .RegisterHandler(async context =>
                {
                    var projectName = context.Input;

                    var dialog = new Views.ConfirmDiscardDialog
                    {
                        Message = $"Save changes to \"{projectName}\"?"
                    };

                    // The buttons in the dialog are bound to the DiscardAction enum values — see the CommandParameter bindings in the XAML.
                    var result = await DialogHost.Show(dialog, "MainDialogHost");

                    context.SetOutput((DiscardAction)result!);
                })
                .DisposeWith(disposables);

            // Exit menu item.
            this.BindCommand(ViewModel, vm => vm.ExitCommand, view => view.ExitMenuItem)
                .DisposeWith(disposables);

            ViewModel!
                .ExitCommand
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            _store
                .WhenAnyValue(store => store.CurrentFilePath)
                .Subscribe(_ => UpdateTitle())
                .DisposeWith(disposables);

            _store
                .WhenAnyValue(store => store.IsDirty)
                .Subscribe(_ => UpdateTitle())
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
    protected override async void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (ViewModel is not null && !ViewModel.CanClose)
        {
            e.Cancel = true;
            return;
        }

        // Prompt before discarding unsaved changes.
        if (_store.IsDirty)
        {
            var action = await ViewModel!.PromptDiscardAsync();

            if (action == DiscardAction.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (action == DiscardAction.Save)
            {
                await ViewModel.SaveCommand.Execute();
            }
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

    private void UpdateTitle()
    {
        if (_store.CurrentFilePath is null)
        {
            Title = "SlnDependencyStudio";
        }
        else
        {
            var name = Path.GetFileNameWithoutExtension(_store.CurrentFilePath);

            Title = _store.IsDirty
                ? $"SlnDependencyStudio — {name} *"
                : $"SlnDependencyStudio — {name}";
        }
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
