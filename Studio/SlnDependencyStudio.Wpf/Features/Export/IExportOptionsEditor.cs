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
}
