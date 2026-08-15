using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
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
internal sealed class ProjectAssetReader : IProjectAssetReader
{
    // Caches parsed project.assets.json lock files by absolute assets-file path so
    // repeated queries within a run do not re-read or re-parse the same file.
    private readonly Dictionary<string, LockFile> _lockFileCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ProjectAssetReader> _logger;

    /// <summary>Initializes a new instance of <see cref="ProjectAssetReader"/>.</summary>
    /// <param name="logger">The logger used for diagnostics.</param>
    public ProjectAssetReader(ILogger<ProjectAssetReader> logger)
    {
        _logger = logger;
    }

    /// <summary>Returns all target frameworks declared in a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The target frameworks defined in the assets file.</returns>
    public string[] GetTargetFrameworks(string projectPath)
    {
        var lockFile = LoadLockFile(projectPath);

        var frameworks = (string[])[.. lockFile.Targets
            .Where(target => target.RuntimeIdentifier.IsNullOrEmpty())
            .Select(target => target.TargetFramework.GetShortFolderName())];

        _logger.LogDebug("Discovered {TargetFrameworkCount} target framework(s) for {ProjectPath}: {TargetFrameworks}",
            frameworks.Length, Path.GetFileName(projectPath), string.Join(", ", frameworks));

        return frameworks;
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

        _logger.LogDebug("Resolved {ExplicitPackageCount} explicit package reference(s) for {ProjectPath} ({TargetFramework})",
            result.Count, Path.GetFileName(projectPath), targetFramework);

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

    // Determines whether a package node's resolved version should be flagged as "requested a
    // different version" in the summary. This drives the per-edge "requested ... resolved ..."
    // lines in the package-conflict table, so it must only return true when there is a GENUINE
    // discrepancy between what the requesting edge asked for and what NuGet actually resolved.
    //
    // The inputs are:
    //   requestedVersionRange - the VersionRange declared by the edge that requested this package.
    //     For an explicit (direct) reference this comes from the project's PackageReference
    //     (LibraryRange.VersionRange); for a transitive reference it is the range declared by the
    //     parent package's dependency on this one.
    //   resolvedVersion      - the version NuGet actually resolved for the package, read from the
    //     project.assets.json library entry (LockFileTargetLibrary.Version).
    //
    // NuGet requests are rarely exact pins. A PackageReference such as
    //     <PackageReference Include="Some.Package" Version=">= 2.0.0" />
    // declares a RANGE, and NuGet resolves the best available version that SATISFIES that range
    // (2.1.0, for example). Resolving within the range is normal, correct behaviour — the resolved
    // version is exactly what the request permitted, so there is NO discrepancy and nothing to
    // flag. Only when NuGet resolves OUTSIDE the requested constraint (an unexpected result, or a
    // conflicting set of transitive constraints) is there a "different version" worth surfacing.
    //
    // NuGet's VersionRange.Satisfies(version) answers precisely that question: "is this version
    // valid under the requested constraint?" So the whole decision collapses to
    //     !requestedVersionRange.Satisfies(resolvedVersion)
    // with exact pins falling out naturally from the same check:
    //
    //   Example 1 - exact pin that matches the resolution (no mismatch):
    //       requested [1.2.3, 1.2.3], resolved 1.2.3   -> Satisfies == true  -> false
    //
    //   Example 2 - exact pin that does NOT match the resolution (a real conflict):
    //       requested [1.2.3, 1.2.3], resolved 1.2.4   -> Satisfies == false -> true
    //       The project pinned 1.2.3 but got 1.2.4, so it genuinely requested a different version.
    //
    //   Example 3 - ranged request resolved WITHIN the range (normal - do NOT add noise):
    //       requested >= 2.0.0,       resolved 2.1.0   -> Satisfies == true  -> false
    //       2.1.0 satisfies the >= 2.0.0 constraint, so NuGet behaved exactly as asked.
    //
    //   Example 4 - ranged request resolved OUTSIDE the range (a genuine discrepancy to surface):
    //       requested [2.0.0, 3.0.0), resolved 3.1.0   -> Satisfies == false -> true
    //       3.1.0 falls outside [2.0.0, 3.0.0), so the edge really requested a different version.
    //
    //   Example 5 - no constraint or no resolved version (nothing to compare):
    //       requested null, resolved 2.1.0             -> false
    //       requested >= 2.0.0, resolved null          -> false
    //
    // A floating range such as "1.*" behaves the same way: Satisfies(1.5.0) is true, so an
    // in-range floating resolution is not flagged.
    //
    /// <summary>Determines whether a resolved package version does not satisfy the version range requested by its parent edge.</summary>
    /// <param name="requestedVersionRange">The version range requested by the parent dependency edge.</param>
    /// <param name="resolvedVersion">The version NuGet resolved for the package.</param>
    /// <returns><see langword="true"/> when the resolved version falls outside the requested range; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRequestedDifferentVersion(VersionRange? requestedVersionRange, NuGetVersion? resolvedVersion)
    {
        return requestedVersionRange is not null &&
               resolvedVersion is not null &&
               !requestedVersionRange.Satisfies(resolvedVersion);
    }

    private static VersionRange? GetRequestedVersionRange(LibraryDependency dependency)
    {
        return dependency.LibraryRange?.VersionRange;
    }

    /// <summary>Loads and caches a project's assets file.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The parsed lock file.</returns>
    /// <exception cref="ProjectAssetsException">Thrown when the assets file is missing or has an unsupported format.</exception>
    private LockFile LoadLockFile(string projectPath)
    {
        var assetsPath = GetAssetsFilePath(projectPath);

        if (_lockFileCache.TryGetValue(assetsPath, out var cached))
        {
            _logger.LogDebug("Assets file cache hit: {AssetsPath}", assetsPath);

            return cached;
        }

        _logger.LogDebug("Loading assets file: {AssetsPath}", assetsPath);

        if (!File.Exists(assetsPath))
        {
            throw new ProjectAssetsException(
                $"Run 'dotnet restore' before generating diagrams. Missing assets file: {assetsPath}");
        }

        var lockFile = LockFileUtilities.GetLockFile(assetsPath, NullLogger.Instance);

        if (lockFile.Version < 3)
        {
            throw new ProjectAssetsException(
                $"The project.assets.json at '{assetsPath}' uses an unsupported format (version {lockFile.Version}). Re-run 'dotnet restore'.");
        }

        _lockFileCache[assetsPath] = lockFile;

        return lockFile;
    }

    /// <summary>Builds the expected assets file path for a project.</summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The assets file path under the project's obj folder.</returns>
    /// <exception cref="ProjectAssetsException">Thrown when the project directory cannot be determined.</exception>
    private static string GetAssetsFilePath(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath)
            ?? throw new ProjectAssetsException($"Cannot determine the directory for project: {projectPath}");

        return Path.Combine(projectDir, "obj", "project.assets.json");
    }
}
