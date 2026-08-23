using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System.Reactive.Disposables.Fluent;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>The "Project" navigation page. Displays and allows editing of project metadata
/// (name and description) from the current dependency project document. Changes are held in
/// the store's editing buffer and only flushed to the document on explicit save.</summary>
public partial class ProjectView : ReactiveUserControl<ProjectViewModel>
{
    public ProjectView(ProjectViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindValidation(
                    ViewModel,
                    vm => vm.ProjectName.Value,
                    view => view.ProjectNameFormField.ValidationError)
                .DisposeWith(disposables);
        });
    }
}
