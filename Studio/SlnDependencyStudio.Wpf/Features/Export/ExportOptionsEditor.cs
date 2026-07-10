using DynamicData.Binding;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorExportOptions"/>.
/// </summary>
internal sealed class ExportOptionsEditor : ReactiveObject, IExportOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private readonly Subject<bool> _imageFormatsDirtySubject = new();
    private readonly ObservableCollectionExtended<DiagramImageFormat> _imageFormatsCollection;
    private DiagramImageFormat[] _originalImageFormats = [];

    /// <inheritdoc />
    public TrackableValue<string> RootPath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> ClearContents { get; } = new();

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<DiagramImageFormat>> ImageFormats { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance of <see cref="ExportOptionsEditor"/>.</summary>
    public ExportOptionsEditor()
    {
        InitializeTrackable(RootPath, string.Empty);
        InitializeTrackable(UseRelativePath, true);
        InitializeTrackable(ClearContents, false);

        _imageFormatsCollection = [];
        InitializeTrackable(ImageFormats, _imageFormatsCollection);

        _imageFormatsCollection.CollectionChanged += OnImageFormatsCollectionChanged;

        _isDirty = Observable
            .CombineLatest(
                RootPath.WhenAnyValue(path => path.IsDirty),
                ClearContents.WhenAnyValue(cc => cc.IsDirty),
                _imageFormatsDirtySubject.StartWith(false),
                (root, clear, imageFormats) => root || clear || imageFormats)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <summary>Populates all TrackableValues from the given options and marks the editor clean.</summary>
    /// <param name="source">The export options to load.</param>
    public void SetOriginalValues(GeneratorExportOptions source)
    {
        RootPath.SetOriginalValue(source.RootPath);
        ClearContents.SetOriginalValue(source.ClearContents);

        _originalImageFormats = source.ImageFormats;
        ImageFormats.SetOriginalValue(_imageFormatsCollection);

        _imageFormatsCollection.Load(source.ImageFormats);
    }

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The export options to mutate.</param>
    public void FlushTo(GeneratorExportOptions target)
    {
        target.RootPath = RootPath.Value;
        target.ClearContents = ClearContents.Value;
        target.ImageFormats = [.. _imageFormatsCollection];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void OnImageFormatsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        var isDirty = _imageFormatsCollection.Count != _originalImageFormats.Length ||
                      !_imageFormatsCollection.OrderBy(format => format).SequenceEqual(_originalImageFormats.OrderBy(format => format));

        _imageFormatsDirtySubject.OnNext(isDirty);
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
