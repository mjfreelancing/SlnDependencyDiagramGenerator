using Microsoft.Win32;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>The "Export" navigation page. Displays and allows editing of the export
/// root path from the current dependency project document. Changes are held in the store's
/// editing buffer and only flushed to the document on explicit save.</summary>
public partial class ExportView : ReactiveUserControl<ExportViewModel>
{
    public ExportView(ExportViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindValidation(
                    ViewModel,
                    vm => vm.RootPath.Value,
                    view => view.ExportRootFormField.ValidationError)
                .DisposeWith(disposables);

            ViewModel!
                .BrowseExportPathInteraction
                .RegisterHandler(ctx =>
                {
                    var resolvedPath = ctx.Input ?? string.Empty;

                    var dialog = new OpenFolderDialog
                    {
                        InitialDirectory = resolvedPath
                    };

                    ctx.SetOutput(dialog.ShowDialog() == true ? dialog.FolderName : null);
                })
                .DisposeWith(disposables);
        });
    }
}
