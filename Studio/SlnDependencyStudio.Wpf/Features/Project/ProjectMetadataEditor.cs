using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>
/// Reactive editing wrapper for <see cref="DependencyProjectMetadata"/>.
/// Mirrors each editable metadata field with a <see cref="TrackableValue{T}"/>
/// and derives its own <see cref="IsDirty"/> state from them.
/// </summary>
internal sealed class ProjectMetadataEditor : ReactiveObject, IProjectMetadataEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableAsPropertyHelper<bool> _isDirty;

    /// <inheritdoc />
    public TrackableValue<string> ProjectName { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Description { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>
    /// Initializes a new instance with all TrackableValues seeded to empty defaults.
    /// This ensures <see cref="IsDirty"/> is valid from construction, before any document is loaded.
    /// </summary>
    public ProjectMetadataEditor()
    {
        InitializeTrackable(ProjectName, string.Empty);
        InitializeTrackable(Description, string.Empty);

        _isDirty = this.WhenAnyValue(
                editor => editor.ProjectName.IsDirty,
                editor => editor.Description.IsDirty,
                (nameDirty, descDirty) => nameDirty || descDirty)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <summary>Populates all TrackableValues from the given metadata and establishes a clean baseline.</summary>
    /// <param name="source">The document metadata to load.</param>
    public void SetOriginalValues(DependencyProjectMetadata source)
    {
        ProjectName.SetOriginalValue(source.ProjectName);
        Description.SetOriginalValue(source.Description);
    }

    /// <summary>Writes current TrackableValue contents back to the given metadata instance.</summary>
    /// <param name="target">The document metadata to mutate.</param>
    public void FlushTo(DependencyProjectMetadata target)
    {
        target.ProjectName = ProjectName.Value;
        target.Description = Description.Value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> tracklable, TValue defaultValue = default!)
    {
        tracklable.SetOriginalValue(defaultValue);
        _disposables.Add(tracklable);
    }
}
