using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>
/// Reactive editing wrapper for <see cref="DependencyProjectMetadata"/>.
/// </summary>
internal sealed class ProjectMetadataEditor : ReactiveObject, IProjectMetadataEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private readonly ILogger<ProjectMetadataEditor> _logger;

    /// <inheritdoc />
    public TrackableValue<string> ProjectName { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Description { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance of <see cref="ProjectMetadataEditor"/>.</summary>
    public ProjectMetadataEditor(ILogger<ProjectMetadataEditor> logger)
    {
        _logger = logger;

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
        _logger.LogDebug("Resetting {Editor}", nameof(ProjectMetadataEditor));

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
