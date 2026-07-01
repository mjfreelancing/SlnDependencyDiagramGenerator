using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>The "Project" navigation page. Displays and allows editing of project metadata
/// (name and description) from the current dependency project document.</summary>
public partial class ProjectView : ReactiveUserControl<ProjectViewModel>
{
    public ProjectView(ProjectViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // When any editable property changes, sync back to the underlying document.
            this.WhenAnyValue(
                    view => view.ViewModel!.ProjectName.Value,
                    view => view.ViewModel!.Description.Value)
                .Subscribe(_ => ViewModel!.ApplyToDocument())
                .DisposeWith(disposables);
        });
    }
}
