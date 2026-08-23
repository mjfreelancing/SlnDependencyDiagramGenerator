using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>
/// Observable editing surface of the <see cref="GeneratorDiagramOptions"/> editor wrapper.
/// Exposes TrackableValue and TrackableCollection properties for XAML binding.
/// </summary>
public interface IDiagramOptionsEditor : IStudioEditor, IStudioSingletonDependency
{
    /// <summary><see langword="true"/> when any tracked value has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the diagram formats collection.</summary>
    TrackableCollection<DiagramFormat> Formats { get; }

    /// <summary>Diagram flow direction (LR/RL/TB/BT).</summary>
    TrackableValue<GeneratorDiagramOptions.DiagramDirection> Direction { get; }

    /// <summary>Framework fill color (hex).</summary>
    TrackableValue<string> FrameworkFill { get; }

    /// <summary>Framework fill opacity (0.0–1.0).</summary>
    TrackableValue<double> FrameworkOpacity { get; }

    /// <summary>Package fill color (hex).</summary>
    TrackableValue<string> PackageFill { get; }

    /// <summary>Package fill opacity (0.0–1.0).</summary>
    TrackableValue<double> PackageOpacity { get; }

    /// <summary>Transitive dependency fill color (hex).</summary>
    TrackableValue<string> TransitiveFill { get; }

    /// <summary>Transitive dependency fill opacity (0.0–1.0).</summary>
    TrackableValue<double> TransitiveOpacity { get; }

    /// <summary>Whether grouping containers are rendered.</summary>
    TrackableValue<bool> GroupingEnabled { get; }

    /// <summary>Group container background fill color (hex).</summary>
    TrackableValue<string> GroupingFill { get; }

    /// <summary>Group container background fill opacity (0.0–1.0).</summary>
    TrackableValue<double> GroupingOpacity { get; }

    /// <summary>The group name (title) for the project grouping.</summary>
    TrackableValue<string> GroupName { get; }

    /// <summary>The alias used for grouping in D2/Mermaid output.</summary>
    TrackableValue<string> GroupNameAlias { get; }

    /// <summary>Populates all TrackableValues from the given options and establishes a clean baseline.</summary>
    /// <param name="source">The diagram options to load.</param>
    void SetOriginalValues(GeneratorDiagramOptions source);

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The diagram options to mutate.</param>
    void FlushTo(GeneratorDiagramOptions target);
}
