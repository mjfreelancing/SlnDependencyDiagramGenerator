using AllOverIt.Extensions;
using NuGet.Common;
using NuGet.ProjectModel;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnDependencyDiagramGenerator.Parser;

// Reads project.assets.json (written by 'dotnet restore') to obtain the fully-resolved
// package graph for each target framework. The assets file is the authoritative post-restore
// source: it embodies NuGet's "nearest wins" conflict resolution, Central Package Management
// (Directory.Packages.props) version pins, and Directory.Build.props property evaluation —
// no special handling for any of these is required here.
/// <summary>Reads project assets files to resolve package dependencies for specific target frameworks.</summary>
internal sealed class ProjectAssetReader
{
    private readonly Dictionary<string, LockFile> _lockFileCache = new(StringComparer.OrdinalIgnoreCase);

    // Returns the target frameworks available in the project's assets file (non-RID targets only).
    /// <summary>Returns all target frameworks declared in a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The target frameworks defined in the assets file.</returns>
    public string[] GetTargetFrameworks(string projectPath)
    {
        var lockFile = LoadLockFile(projectPath);

        return lockFile.Targets
            .Where(target => string.IsNullOrEmpty(target.RuntimeIdentifier))
            .Select(target => target.TargetFramework.GetShortFolderName())
            .ToArray();
    }

    // Returns true if the project's assets file contains the given target framework,
    // including platform-specific variants (e.g. net10.0-windows matches net10.0).
    /// <summary>Checks if a project contains the requested target framework in its assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="targetFramework">The target framework to match.</param>
    /// <returns><see langword="true"/> when the target framework is present; otherwise, <see langword="false"/>.</returns>
    public bool HasTargetFramework(string projectPath, string targetFramework)
    {
        return GetTargetFrameworks(projectPath)
            .Any(framework => IsFrameworkMatch(framework, targetFramework));
    }

    // Reads the fully-resolved package references for the given target framework from the assets file.
    // Explicit packages (directly referenced by the project) are at depth 0; their transitive
    // dependencies are at depth 1, 2, etc. up to maxTransitiveDepth.
    // Any package whose name appears in excludePackages (case-insensitive) is omitted, along with
    // any transitive dependency that is only reachable through excluded packages.
    /// <summary>Reads resolved package references for the given target framework.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="excludePackages">Package IDs to exclude from the results.</param>
    /// <param name="targetFramework">The target framework to resolve.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive depth to include.</param>
    /// <returns>The resolved package tree for direct and transitive dependencies.</returns>
    public IReadOnlyCollection<PackageReference> ReadPackagesForFramework(string projectPath, HashSet<string> excludePackages,
        string targetFramework, int maxTransitiveDepth)
    {
        var lockFile = LoadLockFile(projectPath);

        // Prefer an exact TFM match; fall back to a platform-specific variant with the same
        // base TFM (e.g. net10.0-windows when iterating net10.0).
        var target = GetTarget(lockFile, targetFramework);

        if (target is null)
        {
            return Array.Empty<PackageReference>();
        }

        // Build a lookup of all resolved package libraries for this target framework.
        var packageLibraries = target.Libraries
            .Where(library => string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(library => library.Name, library => library, StringComparer.OrdinalIgnoreCase);

        // Find the explicit (direct) package references for this target framework.
        // These are the packages the project directly references (i.e., PackageReference items
        // in the project file). Project-to-project references are excluded.
        // Same exact-then-base fallback for the PackageSpec entry.
        var packageSpecTf = GetPackageSpecTargetFramework(lockFile, targetFramework);

        var explicitPackageNames = packageSpecTf is not null
            ? packageSpecTf.Dependencies
                .Where(dependency => packageLibraries.ContainsKey(dependency.Name))
                .Select(dependency => dependency.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Build the package tree starting from each explicit package reference.
        var result = new List<PackageReference>();

        foreach (var packageName in explicitPackageNames.OrderBy(package => package, StringComparer.OrdinalIgnoreCase))
        {
            if (excludePackages.Contains(packageName))
            {
                continue;
            }

            var pkg = BuildPackageTree(packageName, packageLibraries, depth: 0, maxDepth: maxTransitiveDepth,
                currentPath: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                excludePackages: excludePackages);

            if (pkg is not null)
            {
                result.Add(pkg);
            }
        }

        return result.AsReadOnlyCollection();
    }

    private static LockFileTarget GetTarget(LockFile lockFile, string targetFramework)
    {
        var frameworkOnlyTargets = lockFile.Targets
            .Where(target => target.RuntimeIdentifier.IsNullOrEmpty())
            .ToArray();

        // Pass 1: prefer an exact target framework match (e.g. net10.0).
        foreach (var target in frameworkOnlyTargets)
        {
            var framework = target.TargetFramework.GetShortFolderName();

            // Stop at the first exact framework match.
            if (IsFrameworkMatch(framework, targetFramework, requireExactMatch: true))
            {
                return target;
            }
        }

        // Pass 2: fall back to base-framework matching (e.g. net10.0-windows -> net10.0).
        foreach (var target in frameworkOnlyTargets)
        {
            var framework = target.TargetFramework.GetShortFolderName();

            // Return the first compatible base-framework match.
            if (IsFrameworkMatch(framework, targetFramework))
            {
                return target;
            }
        }

        return null;
    }

    private static TargetFrameworkInformation GetPackageSpecTargetFramework(LockFile lockFile, string targetFramework)
    {
        // Pass 1: find an exact framework entry from the package spec.
        foreach (var framework in lockFile.PackageSpec.TargetFrameworks)
        {
            var frameworkName = framework.FrameworkName.GetShortFolderName();

            // Stop at the first exact framework match.
            if (IsFrameworkMatch(frameworkName, targetFramework, requireExactMatch: true))
            {
                return framework;
            }
        }

        // Pass 2: fall back to base-framework matching.
        foreach (var framework in lockFile.PackageSpec.TargetFrameworks)
        {
            var frameworkName = framework.FrameworkName.GetShortFolderName();

            // Return the first compatible base-framework match.
            if (IsFrameworkMatch(frameworkName, targetFramework))
            {
                return framework;
            }
        }

        return null;
    }

    private static bool IsFrameworkMatch(string framework, string targetFramework, bool requireExactMatch = false)
    {
        return requireExactMatch
            ? string.Equals(framework, targetFramework, StringComparison.OrdinalIgnoreCase)
            : string.Equals(GetBaseFramework(framework), targetFramework, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseFramework(string framework)
    {
        return framework.Split('-')[0];
    }

    // Recursively builds the package dependency tree from the flat resolved graph.
    // 'currentPath' tracks the active DFS path to detect cycles (which NuGet itself
    // prevents, but are guarded against here for safety).
    /// <summary>Builds a package dependency tree recursively from the resolved package graph.</summary>
    /// <param name="packageName">The package name at the current recursion level.</param>
    /// <param name="libraryLookup">The lookup of package libraries for the target framework.</param>
    /// <param name="depth">The current recursion depth.</param>
    /// <param name="maxDepth">The maximum recursion depth.</param>
    /// <param name="currentPath">The current recursion path used for cycle prevention.</param>
    /// <param name="excludePackages">Package IDs to exclude from the graph.</param>
    /// <returns>A package node when found; otherwise, <see langword="null"/>.</returns>
    private static PackageReference BuildPackageTree(string packageName, Dictionary<string, LockFileTargetLibrary> libraryLookup,
        int depth, int maxDepth, HashSet<string> currentPath, HashSet<string> excludePackages)
    {
        if (!libraryLookup.TryGetValue(packageName, out var library))
        {
            return null;
        }

        var children = new List<PackageReference>();

        // Traverse children only if we have not exceeded maxDepth and are not in a cycle.
        if (depth < maxDepth && currentPath.Add(packageName))
        {
            foreach (var dep in library.Dependencies)
            {
                if (excludePackages.Contains(dep.Id))
                {
                    continue;
                }

                var child = BuildPackageTree(dep.Id, libraryLookup, depth + 1, maxDepth, currentPath, excludePackages);

                if (child is not null)
                {
                    children.Add(child);
                }
            }

            currentPath.Remove(packageName);
        }

        return new PackageReference(isTransitive: depth > 0, depth)
        {
            Name = library.Name,
            Version = library.Version.ToNormalizedString(),
            TransitiveReferences = children.AsReadOnlyCollection()
        };
    }

    // Loads and caches the lock file for a project. Aborts with a clear diagnostic if
    // the file is absent or its format is too old (requires 'dotnet restore' to be re-run).
    /// <summary>Loads and caches a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The parsed lock file.</returns>
    /// <exception cref="DependencyGeneratorException">Thrown when the assets file is missing or has an unsupported format.</exception>
    private LockFile LoadLockFile(string projectPath)
    {
        var assetsPath = GetAssetsFilePath(projectPath);

        if (_lockFileCache.TryGetValue(assetsPath, out var cached))
        {
            return cached;
        }

        if (!File.Exists(assetsPath))
        {
            throw new DependencyGeneratorException(
                $"Run 'dotnet restore' before generating diagrams. Missing assets file: {assetsPath}");
        }

        var lockFile = LockFileUtilities.GetLockFile(assetsPath, NullLogger.Instance);

        if (lockFile.Version < 3)
        {
            throw new DependencyGeneratorException(
                $"The project.assets.json at '{assetsPath}' uses an unsupported format (version {lockFile.Version}). Re-run 'dotnet restore'.");
        }

        _lockFileCache[assetsPath] = lockFile;

        return lockFile;
    }

    /// <summary>Builds the expected assets file path for a project.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The assets file path under the project's obj folder.</returns>
    /// <exception cref="DependencyGeneratorException">Thrown when the project directory cannot be determined.</exception>
    private static string GetAssetsFilePath(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath)
            ?? throw new DependencyGeneratorException($"Cannot determine the directory for project: {projectPath}");

        return Path.Combine(projectDir, "obj", "project.assets.json");
    }
}
