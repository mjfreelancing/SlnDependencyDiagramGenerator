using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorExportOptions"/>.
/// </summary>
internal sealed class ExportOptionsEditor : ReactiveObject, IExportOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private readonly ILogger<ExportOptionsEditor> _logger;

    /// <inheritdoc />
    public TrackableValue<string> RootPath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> ClearContents { get; } = new();

    /// <inheritdoc />
    public TrackableCollection<DiagramImageFormat> ImageFormats { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <summary>Initializes a new instance of <see cref="ExportOptionsEditor"/>.</summary>
    public ExportOptionsEditor(ILogger<ExportOptionsEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(RootPath, string.Empty);
        InitializeTrackable(UseRelativePath, true);
        InitializeTrackable(ClearContents, false);

        _disposables.Add(ImageFormats);

        // Wire up dirty tracking after the trackables are initialized: WhenAnyValue evaluates the
        // expression on subscription, and TrackableValue.IsDirty throws until SetOriginalValue is called.
        _isDirty = WireupIsDirty();
    }

    /// <summary>Populates all TrackableValues from the given options and marks the editor clean.</summary>
    /// <param name="source">The export options to load.</param>
    public void SetOriginalValues(GeneratorExportOptions source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(ExportOptionsEditor));

        RootPath.SetOriginalValue(source.RootPath);
        ClearContents.SetOriginalValue(source.ClearContents);
        ImageFormats.SetOriginalItems(source.ImageFormats);
    }

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The export options to mutate.</param>
    public void FlushTo(GeneratorExportOptions target)
    {
        target.RootPath = RootPath.Value;
        target.ClearContents = ClearContents.Value;
        target.ImageFormats = ImageFormats.ToArray();
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
        var dirtyFlags = new IObservable<bool>[]
        {
            RootPath.WhenAnyValue(trackable => trackable.IsDirty),
            ClearContents.WhenAnyValue(trackable => trackable.IsDirty),
            ImageFormats.WhenAnyValue(trackable => trackable.IsDirty)
        };

        // Dirty state is always false at construction (baselines are set before wiring), so the
        // initial emission adds no information. DistinctUntilChanged logs only genuine transitions
        // and Skip(1) drops the initial value, avoiding an ILogger call during construction.
        var isDirty = Observable
            .CombineLatest(dirtyFlags, flags => flags.Any(isDirty => isDirty))
            .DistinctUntilChanged()
            .Skip(1)
            .Do(isDirty => _logger.LogDebug("{Editor}.IsDirty changed to {IsDirty}", nameof(ExportOptionsEditor), isDirty))
            .ToProperty(this, nameof(IsDirty));

        _disposables.Add(isDirty);

        return isDirty;
    }
}
