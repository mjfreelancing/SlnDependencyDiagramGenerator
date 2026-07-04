// ReactiveUI.Fody attributes ([Reactive], [ObservableAsProperty]) are NOT used in this file.
// Fody's IL weaver cannot process generic type parameters — the backing field for `Value` (a `T`)
// and the property-change notification for `IsDirty` would never be generated, causing
// MissingFieldException at runtime. Both are implemented manually with explicit backing fields
// and RaiseAndSetIfChanged / ObservableAsPropertyHelper<T>.

using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Controls;

/// <summary>
/// A reactive wrapper around a value that tracks whether it has diverged from its
/// original baseline. <see cref="IsDirty"/> is an observable property that emits
/// <see langword="true"/> when <see cref="Value"/> differs from the baseline
/// established by the most recent call to <see cref="SetOriginalValue"/>.
/// </summary>
/// <remarks>
/// Construction does not wire up dirty tracking — call <see cref="SetOriginalValue"/>
/// to establish a baseline and begin observing changes. Calling
/// <see cref="SetOriginalValue"/> again resets the baseline and restarts tracking.
/// </remarks>
public sealed class TrackableValue<T> : ReactiveObject, IDisposable
{
    private T _original = default!;
    private T _value = default!;
    private readonly SerialDisposable _dirtySubscription = new();
    private ObservableAsPropertyHelper<bool> _isDirty = null!;

    public T Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance. Values are uninitialised until
    /// <see cref="SetOriginalValue"/> is called.</summary>
    public TrackableValue()
    {
    }

    /// <summary>Resets the baseline to the given value and marks the tracked value as clean.</summary>
    /// <param name="value">The value to use as both the baseline and the current <see cref="Value"/>.</param>
    public void SetOriginalValue(T value)
    {
        _dirtySubscription.Disposable = null;

        _original = value;
        Value = value;

        _dirtySubscription.Disposable = this.WhenAnyValue(property => property.Value)
            .Select(current => !EqualityComparer<T>.Default.Equals(current, _original))
            .ToProperty(this, name => name.IsDirty, out _isDirty);

        // The new OAPH starts with default(false) but its source observable hasn't emitted
        // yet (Value didn't change, so WhenAnyValue didn't fire). Manually notify downstream
        // observers that IsDirty may have transitioned.
        this.RaisePropertyChanged(nameof(IsDirty));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dirtySubscription.Dispose();
    }
}
