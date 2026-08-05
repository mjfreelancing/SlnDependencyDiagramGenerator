using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;

/// <summary>
/// Observable editing surface of the restore solution editor wrapper.
/// </summary>
public interface IRestoreSolutionEditor : IStudioEditor, IStudioSingletonDependency
{
    /// <summary><see langword="true"/> when the tracked value has diverged from its most recent baseline.</summary>
    bool IsDirty { get; }

    /// <summary>Whether the solution should be restored (via <c>dotnet restore</c>) before generation.</summary>
    TrackableValue<bool> RestoreSolution { get; }

    /// <summary>Sets the restore-solution value and establishes a clean baseline.</summary>
    /// <param name="restoreSolution">The value to load.</param>
    void SetOriginalValues(bool restoreSolution);
}
