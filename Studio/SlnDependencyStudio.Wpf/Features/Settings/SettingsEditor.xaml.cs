using Microsoft.Win32;
using ReactiveUI;
using System.IO;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>Editor control for application settings. Hosted inside <see cref="SettingsWindow"/>.
/// The ViewModel is set by the parent <see cref="SettingsWindow"/> after construction,
/// so both share the same <see cref="SettingsEditorViewModel"/> instance.</summary>
public partial class SettingsEditor : ReactiveUserControl<SettingsEditorViewModel>
{
    /// <summary>Initializes a new instance of <see cref="SettingsEditor"/>.</summary>
    public SettingsEditor(SettingsEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // --- Command bindings ---

            this.BindCommand(ViewModel, vm => vm.BrowseDefaultProjectFolderCommand, view => view.BrowseDefaultProjectFolderButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BrowseD2ToolPathCommand, view => view.BrowseD2ToolPathButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BrowseMmdcToolPathCommand, view => view.BrowseMmdcToolPathButton)
                .DisposeWith(disposables);

            // --- Interaction handlers (View owns the dialogs, ViewModel stays clean) ---

            ViewModel!.BrowseFolder.RegisterHandler(context =>
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select default project folder",
                    UseDescriptionForTitle = true,
                    SelectedPath = context.Input
                };

                var result = dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                    ? dialog.SelectedPath
                    : null;

                context.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel!.BrowseD2Executable.RegisterHandler(context =>
            {
                var result = BrowseForExecutable("Select d2 executable", context.Input);
                context.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel!.BrowseMmdcExecutable.RegisterHandler(context =>
            {
                var result = BrowseForExecutable("Select mmdc executable", context.Input);
                context.SetOutput(result);
            }).DisposeWith(disposables);
        });
    }

    private static string? BrowseForExecutable(string title, string initialPath)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Executable files (*.exe;*.bat;*.ps1;*.cmd)|*.exe;*.bat;*.ps1;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(initialPath);
            dialog.FileName = Path.GetFileName(initialPath);
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
