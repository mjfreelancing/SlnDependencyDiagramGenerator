using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>
/// Observable editing surface of the <see cref="DependencyProjectMetadata"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding.
/// </summary>
public interface IProjectMetadataEditor : IStudioEditor, IStudioSingletonDependency
{
    /// <summary><see langword="true"/> when either field has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the project name.</summary>
    TrackableValue<string> ProjectName { get; }

    /// <summary>Trackable form of the project description.</summary>
    TrackableValue<string> Description { get; }

    /// <summary>Populates all TrackableValues from the given metadata and establishes a clean baseline.</summary>
    /// <param name="source">The document metadata to load.</param>
    void SetOriginalValues(DependencyProjectMetadata source);

    /// <summary>Writes current TrackableValue contents back to the given metadata instance.</summary>
    /// <param name="target">The document metadata to mutate.</param>
    void FlushTo(DependencyProjectMetadata target);
}
