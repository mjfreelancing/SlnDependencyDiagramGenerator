using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Shared.Config;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>View model for the "Project" navigation page. Displays and allows editing of the
/// project metadata (name and description) from the current <see cref="DependencyProjectDocument"/>.</summary>
public sealed class ProjectViewModel : ReactiveObject
{
    private DependencyProjectDocument? _document;

    /// <summary>The project name, bound two-way to <see cref="DependencyProjectMetadata.ProjectName"/>.</summary>
    [Reactive]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>The project description, bound two-way to <see cref="DependencyProjectMetadata.Description"/>.</summary>
    [Reactive]
    public string Description { get; set; } = string.Empty;

    /// <summary>Loads metadata from the given <see cref="DependencyProjectDocument"/> and sets up
    /// two-way synchronisation so that edits on this view model flow back to the document.</summary>
    /// <param name="document">The document whose metadata should be edited.</param>
    public void LoadFrom(DependencyProjectDocument document)
    {
        _document = document;
        ProjectName = document.Metadata.ProjectName;
        Description = document.Metadata.Description;
    }

    /// <summary>Applies the current values back to the underlying document's metadata.
    /// Call this before save operations to ensure the document reflects the latest edits.</summary>
    public void ApplyToDocument()
    {
        if (_document is null)
        {
            return;
        }

        _document.Metadata.ProjectName = ProjectName;
        _document.Metadata.Description = Description;
    }
}
