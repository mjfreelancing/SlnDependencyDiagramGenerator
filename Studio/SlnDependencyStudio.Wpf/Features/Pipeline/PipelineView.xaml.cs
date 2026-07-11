using Microsoft.Win32;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>The "Pipeline" navigation page. Displays pre-generation command configuration
/// and tool detection status.</summary>
public partial class PipelineView : ReactiveUserControl<PipelineViewModel>
{
    private readonly IProjectDocumentStore _store;

    public PipelineView(PipelineViewModel viewModel, IProjectDocumentStore store)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        _store = store;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindValidation(
                    ViewModel,
                    vm => vm.PreGenError,
                    view => view.PreGenerationFormField.ValidationError)
                .DisposeWith(disposables);

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
