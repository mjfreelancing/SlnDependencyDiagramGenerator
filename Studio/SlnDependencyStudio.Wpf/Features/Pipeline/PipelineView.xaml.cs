using ReactiveUI;
using ReactiveUI.Validation.Extensions;
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
            this.BindValidation(
                    ViewModel,
                    vm => vm.PreGenError,
                    view => view.PreGenerationFormField.ValidationError)
                .DisposeWith(disposables);
        });
    }
}
