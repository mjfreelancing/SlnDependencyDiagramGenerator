using Microsoft.Win32;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>The "Pipeline" navigation page. Displays pre-generation command configuration
/// and tool detection status.</summary>
public partial class PipelineView : ReactiveUserControl<PipelineViewModel>
{
    public PipelineView(PipelineViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel!
                .BrowseCommandInteraction
                .RegisterHandler(ctx =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = "Select executable",
                        Filter = "Executables (*.exe;*.cmd;*.bat;*.ps1)|*.exe;*.cmd;*.bat;*.ps1|All files (*.*)|*.*",
                        InitialDirectory = ctx.Input
                    };

                    ctx.SetOutput(dialog.ShowDialog() == true ? dialog.FileName : null);
                })
                .DisposeWith(disposables);

            ViewModel!
                .BrowseWorkingDirectoryInteraction
                .RegisterHandler(ctx =>
                {
                    var dialog = new OpenFolderDialog
                    {
                        Title = "Select working directory",
                        InitialDirectory = ctx.Input
                    };

                    ctx.SetOutput(dialog.ShowDialog() == true ? dialog.FolderName : null);
                })
                .DisposeWith(disposables);
        });
    }
}
