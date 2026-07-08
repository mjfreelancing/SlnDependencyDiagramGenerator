using Microsoft.Win32;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>The "Solution" navigation page. Displays and allows editing of the solution
/// path from the current dependency project document. Changes are held in the store's
/// editing buffer and only flushed to the document on explicit save.</summary>
public partial class SolutionView : ReactiveUserControl<SolutionViewModel>
{
    public SolutionView(SolutionViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindValidation(
                    ViewModel,
                    vm => vm.SolutionPath.Value,
                    view => view.SolutionPathFormField.ValidationError)
                .DisposeWith(disposables);

            ViewModel!
                .BrowseSolutionPathInteraction
                .RegisterHandler(ctx =>
                {
                    var resolvedPath = ctx.Input ?? string.Empty;

                    var dialog = new OpenFileDialog
                    {
                        Filter = "Solution files (*.sln;*.slnx)|*.sln;*.slnx|All files (*.*)|*.*",
                        InitialDirectory = System.IO.Path.GetDirectoryName(resolvedPath) ?? string.Empty,
                        FileName = System.IO.Path.GetFileName(resolvedPath)
                    };

                    ctx.SetOutput(dialog.ShowDialog() == true ? dialog.FileName : null);
                })
                .DisposeWith(disposables);
        });
    }
}
