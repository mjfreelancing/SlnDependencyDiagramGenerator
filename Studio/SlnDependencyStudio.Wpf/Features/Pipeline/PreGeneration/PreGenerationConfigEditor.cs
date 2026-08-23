using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;

/// <summary>
/// Reactive editing wrapper for <see cref="PreGenerationConfig"/>.
/// </summary>
internal sealed class PreGenerationConfigEditor : ReactiveObject, IPreGenerationConfigEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<PreGenerationConfigEditor> _logger;
    private readonly ObservableAsPropertyHelper<bool> _isDirty;

    /// <inheritdoc />
    public TrackableValue<bool> Enabled { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Command { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Arguments { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> WorkingDirectory { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> ContinueOnFailure { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance with empty defaults.</summary>
    public PreGenerationConfigEditor(ILogger<PreGenerationConfigEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(Enabled, false);
        InitializeTrackable(Command, string.Empty);
        InitializeTrackable(Arguments, string.Empty);
        InitializeTrackable(WorkingDirectory, string.Empty);
        InitializeTrackable(UseRelativePath, true);
        InitializeTrackable(ContinueOnFailure, false);

        // Keep the relative-path checkbox truthful while the working directory is edited/typed
        // (debounced via RelativePathSync); document load is covered immediately by SetOriginalValues.
        _disposables.Add(RelativePathSync.Wire(WorkingDirectory, UseRelativePath));

        // Wire up dirty tracking after the trackables are initialized: WhenAnyValue evaluates the
        // expression on subscription, and TrackableValue.IsDirty throws until SetOriginalValue is called.
        _isDirty = WireupIsDirty();
    }

    /// <inheritdoc />
    public void SetOriginalValues(PreGenerationConfig source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(PreGenerationConfigEditor));

        Enabled.SetOriginalValue(source.Enabled);
        Command.SetOriginalValue(source.Command);
        Arguments.SetOriginalValue(source.Arguments);
        WorkingDirectory.SetOriginalValue(source.WorkingDirectory);

        // Keep the relative-path checkbox in sync with the loaded working directory so it can
        // never disagree with the stored value (an absolute WorkingDirectory shows it unchecked).
        UseRelativePath.SetOriginalValue(!Path.IsPathFullyQualified(source.WorkingDirectory));

        ContinueOnFailure.SetOriginalValue(source.ContinueOnFailure);
    }

    /// <inheritdoc />
    public void FlushTo(PreGenerationConfig target)
    {
        target.Enabled = Enabled.Value;
        target.Command = Command.Value;
        target.Arguments = Arguments.Value;
        target.WorkingDirectory = WorkingDirectory.Value;
        target.ContinueOnFailure = ContinueOnFailure.Value;
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
        var dirtyFlags = new[]
        {
            Enabled.WhenAnyValue(enabled => enabled.IsDirty),
            Command.WhenAnyValue(command => command.IsDirty),
            Arguments.WhenAnyValue(arguments => arguments.IsDirty),
            WorkingDirectory.WhenAnyValue(workingDirectory => workingDirectory.IsDirty),
            ContinueOnFailure.WhenAnyValue(continueOnFailure => continueOnFailure.IsDirty)
        };

        // Dirty state is always false at construction (baselines are set before wiring), so the
        // initial emission adds no information. DistinctUntilChanged logs only genuine transitions
        // and Skip(1) drops the initial value, avoiding an ILogger call during construction.
        var isDirty = Observable
            .CombineLatest(dirtyFlags, flags => flags.Any(isDirty => isDirty))
            .DistinctUntilChanged()
            .Skip(1)
            .Do(isDirty => _logger.LogDebug("{Editor}.IsDirty changed to {IsDirty}", nameof(PreGenerationConfigEditor), isDirty))
            .ToProperty(this, nameof(IsDirty));

        _disposables.Add(isDirty);

        return isDirty;
    }
}
