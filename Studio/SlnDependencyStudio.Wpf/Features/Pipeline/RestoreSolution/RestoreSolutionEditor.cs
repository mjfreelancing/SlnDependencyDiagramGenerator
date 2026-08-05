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
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableValue<bool> RestoreSolution { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with the default value.</summary>
    public RestoreSolutionEditor(ILogger<RestoreSolutionEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(RestoreSolution, true);

        var subscription = RestoreSolution
            .WhenAnyValue(restoreSolution => restoreSolution.IsDirty)
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <inheritdoc />
    public void SetOriginalValues(bool restoreSolution)
    {
        _logger.LogDebug("Resetting {Editor}", nameof(RestoreSolutionEditor));

        RestoreSolution.SetOriginalValue(restoreSolution);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void UpdateIsDirty()
    {
        _isDirty = RestoreSolution.IsDirty;

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
