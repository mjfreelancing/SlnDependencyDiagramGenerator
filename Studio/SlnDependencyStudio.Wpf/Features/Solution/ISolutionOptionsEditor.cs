using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>
/// Observable editing surface of the <see cref="GeneratorSolutionOptions"/> editor wrapper.
/// Exposes TrackableValue properties for XAML binding.
/// </summary>
public interface ISolutionOptionsEditor : IStudioEditor, IStudioSingletonDependency
{
    /// <summary><see langword="true"/> when the solution path has diverged from its most recent baseline.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary>Trackable form of the solution path.</summary>
    TrackableValue<string> SolutionPath { get; }

    /// <summary>When <see langword="true"/>, the Browse command stores the path relative to the project
    /// file directory. When <see langword="false"/>, the absolute path is stored.
    /// Defaults to <see langword="true"/>. This is a UI preference, not persisted to the .sds file.</summary>
    TrackableValue<bool> UseRelativePath { get; }

    /// <summary>Trackable form of the regex-to-include patterns.</summary>
    TrackableCollection<string> RegexToInclude { get; }

    /// <summary>Trackable form of the regex-to-exclude patterns.</summary>
    TrackableCollection<string> RegexToExclude { get; }

    /// <summary>Trackable form of the packages-to-exclude list.</summary>
    TrackableCollection<string> PackagesToExclude { get; }

    /// <summary>Trackable form of the frameworks-to-exclude list.</summary>
    TrackableCollection<string> FrameworksToExclude { get; }

    /// <summary>Whether individual-project scope is enabled.</summary>
    TrackableValue<bool> IndividualEnabled { get; }

    /// <summary>Whether individual-project scope includes dependencies.</summary>
    TrackableValue<bool> IndividualIncludeDependencies { get; }

    /// <summary>Transitive depth for individual-project scope (0 = none).</summary>
    TrackableValue<int> IndividualTransitiveDepth { get; }

    /// <summary>Whether all-projects scope is enabled.</summary>
    TrackableValue<bool> AllEnabled { get; }

    /// <summary>Whether all-projects scope includes dependencies.</summary>
    TrackableValue<bool> AllIncludeDependencies { get; }

    /// <summary>Transitive depth for all-projects scope (0 = none).</summary>
    TrackableValue<int> AllTransitiveDepth { get; }

    /// <summary>Populates all TrackableValues from the given options and establishes a clean baseline.</summary>
    /// <param name="source">The solution options to load.</param>
    void SetOriginalValues(GeneratorSolutionOptions source);

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The solution options to mutate.</param>
    void FlushTo(GeneratorSolutionOptions target);
}
