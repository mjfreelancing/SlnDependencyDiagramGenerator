using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>The "Diagrams" navigation page. Displays format toggle buttons for selecting
/// which diagram formats to generate. Changes are held in the store's editing buffer
/// and only flushed to the document on explicit save.</summary>
public partial class DiagramsView : ReactiveUserControl<DiagramsViewModel>
{
    public DiagramsView(DiagramsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindValidation(
                    ViewModel,
                    vm => vm.Formats.Items.Count,
                    view => view.FormatsFormField.ValidationError)
                .DisposeWith(disposables);

            this.BindValidation(
                    ViewModel,
                    vm => vm.StylesHexError,
                    view => view.StylesFormField.ValidationError)
                .DisposeWith(disposables);

            this.BindValidation(
                    ViewModel,
                    vm => vm.GroupingHexError,
                    view => view.GroupingFormField.ValidationError)
                .DisposeWith(disposables);
        });
    }
}
