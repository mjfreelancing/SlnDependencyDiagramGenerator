using System;
using System.Collections;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Utils;

/// <summary>
/// A read-only wrapper around an array that provides structural (element-wise) equality and a
/// content-derived hash code, so it can be used as a component of a record-based key. A plain
/// array compares by reference, and <see cref="System.Collections.Immutable.ImmutableArray{T}"/>
/// does the same (its equality is reference equality on the backing array); this wrapper compares
/// length and content.
/// </summary>
/// <typeparam name="TType">The array element type.</typeparam>
internal readonly struct EquatableArray<TType> : IEquatable<EquatableArray<TType>>, IReadOnlyList<TType>
{
    private readonly TType[]? _items;

    /// <summary>Initializes a new instance of <see cref="EquatableArray{T}"/>.</summary>
    /// <param name="items">The array to wrap. A <see langword="null"/> value is allowed.</param>
    public EquatableArray(TType[]? items)
    {
        _items = items;
    }

    /// <summary>Gets the number of elements in the array.</summary>
    public int Count => _items?.Length ?? 0;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    public TType this[int index] => (_items ?? Array.Empty<TType>())[index];

    /// <inheritdoc />
    public bool Equals(EquatableArray<TType> other)
    {
        if (ReferenceEquals(_items, other._items))
        {
            return true;
        }

        if (_items is null || other._items is null || _items.Length != other._items.Length)
        {
            return false;
        }

        for (var index = 0; index < _items.Length; index++)
        {
            if (!EqualityComparer<TType>.Default.Equals(_items[index], other._items[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<TType> other && Equals(other);
    }

    /// <summary>Returns a hash code derived from the array content.</summary>
    public override int GetHashCode()
    {
        if (_items is null)
        {
            return 0;
        }

        var hashCode = new HashCode();

        foreach (var item in _items)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public IEnumerator<TType> GetEnumerator()
        => ((IEnumerable<TType>)(_items ?? Array.Empty<TType>())).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>Returns the wrapped elements as a new array.</summary>
    public TType[] ToArray()
        => _items is null ? Array.Empty<TType>() : (TType[])_items.Clone();
}
