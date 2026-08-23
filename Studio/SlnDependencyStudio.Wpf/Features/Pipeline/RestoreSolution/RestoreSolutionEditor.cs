using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;

/// <summary>
/// Reactive editing wrapper for the document's <c>RestoreSolution</c> flag.
/// </summary>
internal sealed class RestoreSolutionEditor : ReactiveObject, IRestoreSolutionEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<RestoreSolutionEditor> _logger;
    private readonly ObservableAsPropertyHelper<bool> _isDirty;

    /// <inheritdoc />
    public TrackableValue<bool> RestoreSolution { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance with the default value.</summary>
    public RestoreSolutionEditor(ILogger<RestoreSolutionEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(RestoreSolution, true);

        // Wire up dirty tracking after the trackables are initialized: WhenAnyValue evaluates the
        // expression on subscription, and TrackableValue.IsDirty throws until SetOriginalValue is called.
        _isDirty = WireupIsDirty();
    }

    /// <inheritdoc />
    public void SetOriginalValues(bool restoreSolution)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(RestoreSolutionEditor));

        RestoreSolution.SetOriginalValue(restoreSolution);
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
        // Dirty state is always false at construction (baselines are set before wiring), so the
        // initial emission adds no information. DistinctUntilChanged logs only genuine transitions
        // and Skip(1) drops the initial value, avoiding an ILogger call during construction.
        var isDirty = RestoreSolution
            .WhenAnyValue(restoreSolution => restoreSolution.IsDirty)
            .DistinctUntilChanged()
            .Skip(1)
            .Do(isDirty => _logger.LogDebug("{Editor}.IsDirty changed to {IsDirty}", nameof(RestoreSolutionEditor), isDirty))
            .ToProperty(this, nameof(IsDirty));

        _disposables.Add(isDirty);

        return isDirty;
    }
}
