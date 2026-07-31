using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;

/// <summary>
/// Read-only observable surface of the restore solution editor wrapper.
/// </summary>
public interface IRestoreSolutionEditor
{
    /// <summary><see langword="true"/> when the tracked value has diverged from its most recent baseline.</summary>
    bool IsDirty { get; }

    /// <summary>Whether the solution should be restored (via <c>dotnet restore</c>) before generation.</summary>
    TrackableValue<bool> RestoreSolution { get; }
}
