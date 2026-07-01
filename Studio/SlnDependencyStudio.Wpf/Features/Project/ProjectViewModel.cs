using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>View model for the "Project" navigation page. Displays and allows editing of the
/// project metadata (name and description) from the current <see cref="DependencyProjectDocument"/>.</summary>
public sealed class ProjectViewModel : ReactiveObject
{
    private DependencyProjectDocument? _document;
    private readonly SerialDisposable _dirtyTracking = new();

    /// <summary>The project name. Bound via <c>ProjectName.Value</c> in XAML.</summary>
    public TrackableValue<string> ProjectName { get; } = new();

    /// <summary>The project description. Bound via <c>Description.Value</c> in XAML.</summary>
    public TrackableValue<string> Description { get; } = new();

    /// <summary><see langword="true"/> when any of the Project attributes, such as <see cref="ProjectName"/>
    /// or <see cref="Description"/>, has diverged from its original value.</summary>
    [Reactive]
    public bool IsDirty { get; set; }

    public ProjectViewModel()
    {
    }

    /// <summary>Loads metadata from the given <see cref="DependencyProjectDocument"/>.</summary>
    /// <param name="document">The document whose metadata should be edited.</param>
    public void LoadFrom(DependencyProjectDocument document)
    {
        _document = document;

        ProjectName.SetOriginalValue(document.Metadata.ProjectName);
        Description.SetOriginalValue(document.Metadata.Description);

        _dirtyTracking.Disposable = this.WhenAnyValue(
                vm => vm.ProjectName.IsDirty,
                vm => vm.Description.IsDirty,
                (nameDirty, descDirty) => nameDirty || descDirty)
            .BindTo(this, vm => vm.IsDirty);
    }

    /// <summary>Applies the current values back to the underlying document's metadata.</summary>
    public void ApplyToDocument()
    {
        if (_document is null)
        {
            return;
        }

        _document.Metadata.ProjectName = ProjectName.Value;
        _document.Metadata.Description = Description.Value;
    }

    /// <summary>Resets all <see cref="TrackableValue{T}"/> baselines to their current values
    /// so <see cref="IsDirty"/> returns <see langword="false"/>. Called after save.</summary>
    public void MarkClean()
    {
        ProjectName.SetOriginalValue(ProjectName.Value);
        Description.SetOriginalValue(Description.Value);
    }
}
