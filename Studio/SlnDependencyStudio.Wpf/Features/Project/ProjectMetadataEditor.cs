using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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

        // Wire up dirty tracking after the trackables are initialized: WhenAnyValue evaluates the
        // expression on subscription, and TrackableValue.IsDirty throws until SetOriginalValue is called.
        _isDirty = WireupIsDirty();
    }

    /// <summary>Populates all TrackableValues from the given metadata and establishes a clean baseline.</summary>
    /// <param name="source">The document metadata to load.</param>
    public void SetOriginalValues(DependencyProjectMetadata source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(ProjectMetadataEditor));

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

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }

    private ObservableAsPropertyHelper<bool> WireupIsDirty()
    {
        var dirtyFlags = new IObservable<bool>[]
        {
            ProjectName.WhenAnyValue(trackable => trackable.IsDirty),
            Description.WhenAnyValue(trackable => trackable.IsDirty)
        };

        // Dirty state is always false at construction (baselines are set before wiring), so the
        // initial emission adds no information. DistinctUntilChanged logs only genuine transitions
        // and Skip(1) drops the initial value, avoiding an ILogger call during construction.
        var isDirty = Observable
            .CombineLatest(dirtyFlags, flags => flags.Any(isDirty => isDirty))
            .DistinctUntilChanged()
            .Skip(1)
            .Do(isDirty => _logger.LogDebug("{Editor}.IsDirty changed to {IsDirty}", nameof(ProjectMetadataEditor), isDirty))
            .ToProperty(this, nameof(IsDirty));

        _disposables.Add(isDirty);

        return isDirty;
    }
}
