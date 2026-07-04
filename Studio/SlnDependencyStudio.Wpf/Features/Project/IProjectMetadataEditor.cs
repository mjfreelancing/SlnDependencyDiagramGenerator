using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>
/// Read-only observable surface of the <see cref="DependencyProjectMetadata"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding without revealing internal
/// lifecycle methods (<c>SetOriginalValues</c>, <c>FlushTo</c>).
/// </summary>
public interface IProjectMetadataEditor
{
    /// <summary><see langword="true"/> when either field has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the project name.</summary>
    TrackableValue<string> ProjectName { get; }

    /// <summary>Trackable form of the project description.</summary>
    TrackableValue<string> Description { get; }
}
