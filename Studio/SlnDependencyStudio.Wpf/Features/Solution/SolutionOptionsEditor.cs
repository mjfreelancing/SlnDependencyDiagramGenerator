using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorSolutionOptions"/>.
/// Mirrors each editable field with a <see cref="TrackableValue{T}"/>
/// and derives its own <see cref="IsDirty"/> state from them.
/// </summary>
internal sealed class SolutionOptionsEditor : ReactiveObject, ISolutionOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableAsPropertyHelper<bool> _isDirty;

    /// <inheritdoc />
    public TrackableValue<string> SolutionPath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>
    /// Initializes a new instance with all TrackableValues seeded to empty defaults.
    /// This ensures <see cref="IsDirty"/> is valid from construction, before any document is loaded.
    /// </summary>
    public SolutionOptionsEditor()
    {
        InitializeTrackable(SolutionPath, string.Empty);
        InitializeTrackable(UseRelativePath, true);

        _isDirty = SolutionPath
            .WhenAnyValue(p => p.IsDirty)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <summary>Populates all TrackableValues from the given options and establishes a clean baseline.</summary>
    /// <param name="source">The solution options to load.</param>
    public void SetOriginalValues(GeneratorSolutionOptions source)
    {
        SolutionPath.SetOriginalValue(source.SolutionPath);
    }

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The solution options to mutate.</param>
    public void FlushTo(GeneratorSolutionOptions target)
    {
        target.SolutionPath = SolutionPath.Value;
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
}
