using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Collections.ObjectModel;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>
/// Read-only observable surface of the <see cref="GeneratorDiagramOptions"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding.
/// </summary>
public interface IDiagramOptionsEditor
{
    /// <summary><see langword="true"/> when any tracked value has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the diagram formats collection.
    /// Items are added/removed to reflect the user's format selection.</summary>
    TrackableValue<ObservableCollection<DiagramFormat>> Formats { get; }
}
