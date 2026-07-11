using DynamicData.Binding;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SlnDependencyStudio.Wpf.Controls;

/// <summary>
/// A reactive wrapper around an observable collection that tracks whether it has
/// diverged from its original baseline. <see cref="IsDirty"/> emits <see langword="true"/>
/// whenever the collection contents differ from the most recent
/// <see cref="SetOriginalItems"/> snapshot. Comparisons are order-independent.
/// </summary>
/// <typeparam name="TValue">The item type.</typeparam>
public sealed class TrackableCollection<TValue> : ReactiveObject, IDisposable
{
    private readonly ObservableCollectionExtended<TValue> _items;
    private TValue[] _originalItems = [];
    private bool _isDirty;

    /// <summary>
    /// The live collection. Mutations (add, remove, clear) are detected and
    /// drive the <see cref="IsDirty"/> computation.
    /// </summary>
    public ObservableCollection<TValue> Items => _items;

    /// <summary>
    /// <see langword="true"/> when <see cref="Items"/> differs (order-independent)
    /// from the baseline set by the most recent call to <see cref="SetOriginalItems"/>.
    /// </summary>
    /// <remarks>
    /// This property is observable.
    /// </remarks>
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with an empty collection.</summary>
    public TrackableCollection()
    {
        _items = [];
        _items.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>Resets the baseline and replaces all items in a single notification.</summary>
    /// <param name="items">The items to use as both the snapshot and the current contents.</param>
    public void SetOriginalItems(IEnumerable<TValue> items)
    {
        var itemsArray = items.ToArray();

        _originalItems = itemsArray;
        _items.Load(itemsArray);

        _isDirty = false;
        this.RaisePropertyChanged(nameof(IsDirty));
    }

    /// <summary>Replaces all items in a single Reset notification
    /// (does not change the original snapshot).</summary>
    public void Load(IEnumerable<TValue> items)
    {
        _items.Load(items);
    }

    /// <summary>Returns the current items as an array.</summary>
    public TValue[] ToArray() => [.. _items];

    /// <inheritdoc />
    public void Dispose()
    {
        _items.CollectionChanged -= OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _isDirty = _items.Count != _originalItems.Length ||
                   !_items.OrderBy(item => item).SequenceEqual(_originalItems.OrderBy(item => item));

        this.RaisePropertyChanged(nameof(IsDirty));
    }
}
