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

        _isDirty = Observable
            .CombineLatest(
                RootPath.WhenAnyValue(path => path.IsDirty),
                ClearContents.WhenAnyValue(contents => contents.IsDirty),
                ImageFormats.WhenAnyValue(formats => formats.IsDirty),
                (pathDirty, contentsDirty, formatsDirty) => pathDirty || contentsDirty || formatsDirty)
            .ToProperty(this, nameof(IsDirty));
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
}
