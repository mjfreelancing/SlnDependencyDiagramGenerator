using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;

/// <summary>
/// Reactive editing wrapper for <see cref="PostGenerationConfig"/>.
/// </summary>
internal sealed class PostGenerationConfigEditor : ReactiveObject, IPostGenerationConfigEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<PostGenerationConfigEditor> _logger;
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
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with empty defaults.</summary>
    public PostGenerationConfigEditor(ILogger<PostGenerationConfigEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(Enabled, false);
        InitializeTrackable(Command, string.Empty);
        InitializeTrackable(Arguments, string.Empty);
        InitializeTrackable(WorkingDirectory, string.Empty);

        var dirtyFlags = new[]
        {
            Enabled.WhenAnyValue(enabled => enabled.IsDirty),
            Command.WhenAnyValue(command => command.IsDirty),
            Arguments.WhenAnyValue(arguments => arguments.IsDirty),
            WorkingDirectory.WhenAnyValue(workingDirectory => workingDirectory.IsDirty)
        };

        var subscription = Observable
            .CombineLatest(dirtyFlags)
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <inheritdoc />
    public void SetOriginalValues(PostGenerationConfig source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(PostGenerationConfigEditor));

        Enabled.SetOriginalValue(source.Enabled);
        Command.SetOriginalValue(source.Command);
        Arguments.SetOriginalValue(source.Arguments);
        WorkingDirectory.SetOriginalValue(source.WorkingDirectory);
    }

    /// <inheritdoc />
    public void FlushTo(PostGenerationConfig target)
    {
        target.Enabled = Enabled.Value;
        target.Command = Command.Value;
        target.Arguments = Arguments.Value;
        target.WorkingDirectory = WorkingDirectory.Value;
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
                   WorkingDirectory.IsDirty;

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
