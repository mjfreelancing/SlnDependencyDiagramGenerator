using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>
/// Read-only observable surface of the <see cref="GeneratorSolutionOptions"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding.
/// </summary>
public interface ISolutionOptionsEditor
{
    /// <summary><see langword="true"/> when the solution path has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the solution path.</summary>
    TrackableValue<string> SolutionPath { get; }
}
