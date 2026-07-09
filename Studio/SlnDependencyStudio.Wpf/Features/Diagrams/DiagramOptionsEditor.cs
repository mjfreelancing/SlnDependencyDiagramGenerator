using DynamicData.Binding;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorDiagramOptions"/>.
/// Mirrors each editable field with a <see cref="TrackableValue{T}"/>.
/// Uses <see cref="ObservableCollectionExtended{T}"/> for batch collection operations
/// that fire a single <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Reset"/> event.
/// </summary>
internal sealed class DiagramOptionsEditor : ReactiveObject, IDiagramOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableCollectionExtended<DiagramFormat> _formatsCollection;
    private DiagramFormat[] _originalFormats = [];
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<DiagramFormat>> Formats { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Initializes a new instance with all TrackableValues seeded to empty defaults.
    /// This ensures <see cref="IsDirty"/> is valid from construction, before any document is loaded.
    /// </summary>
    public DiagramOptionsEditor()
    {
        _formatsCollection = [];
        InitializeTrackable(Formats, _formatsCollection);

        _formatsCollection.CollectionChanged += OnFormatsCollectionChanged;
    }

    /// <summary>Populates the Formats collection from the given options and marks the editor clean.
    /// Uses <c>Load</c> to replace all items in a single Reset event, so the ViewModel
    /// receives exactly one notification after the collection matches the baseline.</summary>
    /// <param name="source">The diagram options to load.</param>
    public void SetOriginalValues(GeneratorDiagramOptions source)
    {
        _originalFormats = source.Formats;
        Formats.SetOriginalValue(_formatsCollection);

        _formatsCollection.Load(source.Formats);
    }

    /// <summary>Writes current Formats collection contents back to the given options instance.</summary>
    /// <param name="target">The diagram options to mutate.</param>
    public void FlushTo(GeneratorDiagramOptions target)
    {
        target.Formats = [.. _formatsCollection];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void OnFormatsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _isDirty = _formatsCollection.Count != _originalFormats.Length ||
                   !_formatsCollection.OrderBy(format => format).SequenceEqual(_originalFormats.OrderBy(format => format));

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
