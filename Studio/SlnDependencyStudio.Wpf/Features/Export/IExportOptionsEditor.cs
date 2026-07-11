using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>
/// Read-only observable surface of the <see cref="GeneratorExportOptions"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding.
/// </summary>
public interface IExportOptionsEditor
{
    /// <summary><see langword="true"/> when any tracked value has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the export root path.</summary>
    TrackableValue<string> RootPath { get; }

    /// <summary>When <see langword="true"/>, the Browse command stores the path relative to the project
    /// file directory. When <see langword="false"/>, the absolute path is stored.
    /// Defaults to <see langword="true"/>. This is a UI preference, not persisted to the .sds file.</summary>
    TrackableValue<bool> UseRelativePath { get; }

    /// <summary>When <see langword="true"/>, clears the output folder before generating new files.</summary>
    TrackableValue<bool> ClearContents { get; }

    /// <summary>Trackable form of the image format collection.
    /// Items are added/removed to reflect the user's format selection.</summary>
    TrackableCollection<DiagramImageFormat> ImageFormats { get; }
}
