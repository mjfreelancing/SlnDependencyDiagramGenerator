using AllOverIt.Extensions;
using NuGet.Common;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Versioning;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Reads project assets files to resolve package dependencies for specific target frameworks.</summary>
/// <remarks>
/// Reads <c>project.assets.json</c> (written by <c>dotnet restore</c>) to obtain the fully resolved
/// package graph for each target framework. The assets file is the authoritative post-restore source,
/// already reflecting NuGet conflict resolution, Central Package Management
/// (<c>Directory.Packages.props</c>), and <c>Directory.Build.props</c> evaluation.
/// </remarks>
internal sealed class ProjectAssetReader
{
    // Caches parsed project.assets.json lock files by absolute assets-file path so
    // repeated queries within a run do not re-read or re-parse the same file.
    private readonly Dictionary<string, LockFile> _lockFileCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns all target frameworks declared in a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The target frameworks defined in the assets file.</returns>
    public string[] GetTargetFrameworks(string projectPath)
    {
        var lockFile = LoadLockFile(projectPath);

        return [.. lockFile.Targets
            .Where(target => target.RuntimeIdentifier.IsNullOrEmpty())
            .Select(target => target.TargetFramework.GetShortFolderName())];
    }

    /// <summary>Checks if a project contains the requested target framework in its assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="targetFramework">The target framework to match.</param>
    /// <returns><see langword="true"/> when the target framework is present; otherwise, <see langword="false"/>.</returns>
    public bool HasTargetFramework(string projectPath, string targetFramework)
    {
        return GetTargetFrameworks(projectPath).Any(framework => IsFrameworkMatch(framework, targetFramework));
    }

    /// <summary>Reads resolved package references for the given target framework.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="excludePackages">Package IDs to exclude from the results.</param>
    /// <param name="targetFramework">The target framework to resolve.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive depth to include.</param>
    /// <returns>The resolved package tree for direct and transitive dependencies.</returns>
    public PackageReference[] ReadPackagesForFramework(string projectPath, HashSet<string> excludePackages,
        string targetFramework, int maxTransitiveDepth)
    {
        var lockFile = LoadLockFile(projectPath);

        // Prefer an exact TFM match, then fall back to a compatible platform variant.
        var target = GetTarget(lockFile, targetFramework);

        if (target is null)
        {
            return [];
        }

        // Build a lookup of all resolved package libraries for this target framework.
        // NuGet's LockFileTargetLibrary.Name is annotated as string? but resolved packages always have names.
        var packageLibraries = target.Libraries
            .Where(library => string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase) && library.Name is not null)
            .ToDictionary(library => library.Name!, library => library!, StringComparer.OrdinalIgnoreCase);

        // Find explicit (direct) package references for this target framework.
        // Project-to-project references are not part of this package set.
        // Apply the same exact-then-compatible fallback against PackageSpec.
        var packageSpecTargetFramework = GetPackageSpecTargetFramework(lockFile, targetFramework);

        var explicitPackageDependencies = packageSpecTargetFramework is not null
            ? packageSpecTargetFramework.Dependencies
                .Where(dependency => packageLibraries.ContainsKey(dependency.Name))
                .ToDictionary(dependency => dependency.Name, GetRequestedVersionRange, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, VersionRange?>(StringComparer.OrdinalIgnoreCase);

        // Build a package tree from each explicit package reference.
        var result = new List<PackageReference>();

        var orderedPackageNames = explicitPackageDependencies.Keys.OrderBy(package => package, StringComparer.OrdinalIgnoreCase);

        foreach (var packageName in orderedPackageNames)
        {
            if (excludePackages.Contains(packageName))
            {
                continue;
            }

            var requestedVersionRange = explicitPackageDependencies[packageName];

            var packageReference = BuildPackageTree(packageName, packageLibraries, 0, maxTransitiveDepth,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), excludePackages, requestedVersionRange);

            if (packageReference is not null)
            {
                result.Add(packageReference);
            }
        }

        return [.. result];
    }

    private static LockFileTarget? GetTarget(LockFile lockFile, string targetFramework)
    {
        var frameworkOnlyTargets = lockFile.Targets
            .Where(target => target.RuntimeIdentifier.IsNullOrEmpty())
            .ToArray();

        // Pass 1: prefer an exact target framework match.
        foreach (var target in frameworkOnlyTargets)
        {
            var framework = target.TargetFramework.GetShortFolderName();

            // Stop at the first exact match.
            if (IsFrameworkMatch(framework, targetFramework, requireExactMatch: true))
            {
                return target;
            }
        }

        // Pass 2: fall back to compatible base-framework matching.
        foreach (var target in frameworkOnlyTargets)
        {
            var framework = target.TargetFramework.GetShortFolderName();

            // Return the first compatible match.
            if (IsFrameworkMatch(framework, targetFramework))
            {
                return target;
            }
        }

        return null;
    }

    private static TargetFrameworkInformation? GetPackageSpecTargetFramework(LockFile lockFile, string targetFramework)
    {
        // Pass 1: find an exact framework entry from PackageSpec.
        foreach (var framework in lockFile.PackageSpec.TargetFrameworks)
        {
            var frameworkName = framework.FrameworkName.GetShortFolderName();

            // Stop at the first exact match.
            if (IsFrameworkMatch(frameworkName, targetFramework, requireExactMatch: true))
            {
                return framework;
            }
        }

        // Pass 2: fall back to compatible base-framework matching.
        foreach (var framework in lockFile.PackageSpec.TargetFrameworks)
        {
            var frameworkName = framework.FrameworkName.GetShortFolderName();

            // Return the first compatible match.
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

    private static string GetBaseFramework(string framework) => framework.Split('-')[0];

    /// <summary>Builds a package dependency tree recursively from the resolved package graph.</summary>
    /// <param name="packageName">The package name at the current recursion level.</param>
    /// <param name="libraryLookup">The lookup of package libraries for the target framework.</param>
    /// <param name="depth">The current recursion depth.</param>
    /// <param name="maxDepth">The maximum recursion depth.</param>
    /// <param name="activePathPackages">The package IDs currently in the active recursion path, used for cycle prevention.</param>
    /// <param name="excludePackages">Package IDs to exclude from the graph.</param>
    /// <param name="requestedVersionRange">The version range requested by the parent dependency edge.</param>
    /// <returns>A package node when found; otherwise, <see langword="null"/>.</returns>
    private static PackageReference? BuildPackageTree(string packageName, Dictionary<string, LockFileTargetLibrary> libraryLookup,
        int depth, int maxDepth, HashSet<string> activePathPackages, HashSet<string> excludePackages, VersionRange? requestedVersionRange)
    {
        if (!libraryLookup.TryGetValue(packageName, out var library))
        {
            return null;
        }

        var children = new List<PackageReference>();

        // Traverse children only when still within maxDepth and not revisiting this path
        // (cycle prevention for defensive safety).
        if (depth < maxDepth && activePathPackages.Add(packageName))
        {
            foreach (var dep in library.Dependencies)
            {
                if (excludePackages.Contains(dep.Id))
                {
                    continue;
                }

                var child = BuildPackageTree(dep.Id, libraryLookup, depth + 1, maxDepth, activePathPackages, excludePackages,
                    dep.VersionRange);

                if (child is not null)
                {
                    children.Add(child);
                }
            }

            activePathPackages.Remove(packageName);
        }

        // library.Name and library.Version are annotated as nullable by NuGet but always present for resolved packages.
        return new PackageReference(isTransitive: depth > 0, depth)
        {
            Name = library.Name!,
            Version = library.Version!.ToNormalizedString(),
            RequestedVersionRange = requestedVersionRange?.ToString(),
            RequestedDifferentVersion = IsRequestedDifferentVersion(requestedVersionRange, library.Version),
            TransitiveReferences = [.. children]
        };
    }

    private static bool IsRequestedDifferentVersion(VersionRange? requestedVersionRange, NuGetVersion? resolvedVersion)
    {
        if (requestedVersionRange is null || resolvedVersion is null)
        {
            return false;
        }

        // Report exact version pins only when the requested version differs.
        var isExactVersionRequest = requestedVersionRange.MinVersion is not null &&
            requestedVersionRange.MaxVersion is not null &&
            requestedVersionRange.IsMinInclusive &&
            requestedVersionRange.IsMaxInclusive &&
            requestedVersionRange.MinVersion == requestedVersionRange.MaxVersion;

        if (isExactVersionRequest)
        {
            return requestedVersionRange.MinVersion != resolvedVersion;
        }

        // For version ranges, preserve request-path context in the summary to explain
        // what constraints fed into the final NuGet resolution.
        return true;
    }

    private static VersionRange? GetRequestedVersionRange(LibraryDependency dependency)
    {
        return dependency.LibraryRange?.VersionRange;
    }

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
