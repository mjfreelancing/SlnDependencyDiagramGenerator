using AllOverIt.ReactiveUI.Factories;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>Dialog window for editing application settings.
/// Hosts a <see cref="SettingsEditor"/> and owns Save/Cancel buttons via <see cref="SettingsWindowViewModel"/>.</summary>
public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
    /// <summary>Initializes a new instance of <see cref="SettingsWindow"/>.</summary>
    public SettingsWindow(SettingsWindowViewModel viewModel, IViewFactory viewFactory)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        // Use the IViewFactory to create the SettingsEditor, and make sure the SettingsEditorViewModel on the view model
        // for this window is set to the same instance as the SettingsEditor's view model. This ensures that both
        // the window and the view share the same editing state.
        var settingsView = (SettingsEditor)viewFactory.CreateViewFor<SettingsEditorViewModel>();
        viewModel.SettingsEditorViewModel = settingsView.ViewModel!;

        SettingsContent.Content = settingsView;

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.SaveCommand, view => view.SaveButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton)
                .DisposeWith(disposables);

            // When Save completes, close the dialog.
            ViewModel!
                .SaveCommand
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            // When Cancel executes, close the dialog.
            ViewModel!
                .CancelCommand
                .Subscribe(_ => Close())
                .DisposeWith(disposables);
        });
    }
}
