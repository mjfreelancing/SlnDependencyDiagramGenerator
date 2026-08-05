using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
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
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableValue<bool> Enabled { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Command { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> Arguments { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> WorkingDirectory { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> ContinueOnFailure { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with empty defaults.</summary>
    public PreGenerationConfigEditor(ILogger<PreGenerationConfigEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(Enabled, false);
        InitializeTrackable(Command, string.Empty);
        InitializeTrackable(Arguments, string.Empty);
        InitializeTrackable(WorkingDirectory, string.Empty);
        InitializeTrackable(ContinueOnFailure, false);

        var dirtyFlags = new[]
        {
            Enabled.WhenAnyValue(enabled => enabled.IsDirty),
            Command.WhenAnyValue(command => command.IsDirty),
            Arguments.WhenAnyValue(arguments => arguments.IsDirty),
            WorkingDirectory.WhenAnyValue(workingDirectory => workingDirectory.IsDirty),
            ContinueOnFailure.WhenAnyValue(continueOnFailure => continueOnFailure.IsDirty)
        };

        var subscription = Observable
            .CombineLatest(dirtyFlags)
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <inheritdoc />
    public void SetOriginalValues(PreGenerationConfig source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(PreGenerationConfigEditor));

        Enabled.SetOriginalValue(source.Enabled);
        Command.SetOriginalValue(source.Command);
        Arguments.SetOriginalValue(source.Arguments);
        WorkingDirectory.SetOriginalValue(source.WorkingDirectory);
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

    private void UpdateIsDirty()
    {
        _isDirty = Enabled.IsDirty ||
                   Command.IsDirty ||
                   Arguments.IsDirty ||
                   WorkingDirectory.IsDirty ||
                   ContinueOnFailure.IsDirty;

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
