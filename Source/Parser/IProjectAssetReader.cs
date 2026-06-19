using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Provides access to target-framework and package dependency data from project assets files.</summary>
public interface IProjectAssetReader
{
    /// <summary>Returns all target frameworks declared in a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The target frameworks defined in the assets file.</returns>
    string[] GetTargetFrameworks(string projectPath);

    /// <summary>Checks if a project contains the requested target framework in its assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="targetFramework">The target framework to match.</param>
    /// <returns><see langword="true"/> when the target framework is present; otherwise, <see langword="false"/>.</returns>
    bool HasTargetFramework(string projectPath, string targetFramework);

    /// <summary>Reads resolved package references for the given target framework.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="excludePackages">Package IDs to exclude from the results.</param>
    /// <param name="targetFramework">The target framework to resolve.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive depth to include.</param>
    /// <returns>The resolved package tree for direct and transitive dependencies.</returns>
    PackageReference[] ReadPackagesForFramework(string projectPath, HashSet<string> excludePackages,
        string targetFramework, int maxTransitiveDepth);
}