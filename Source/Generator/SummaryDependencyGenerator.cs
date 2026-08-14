using AllOverIt.Extensions;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Creates the markdown dependency summary report.</summary>
internal static class SummaryDependencyGenerator
{
    private sealed record ConflictEntry(string ProjectName, string Version, string[] RequestedVersionPaths);

    /// <summary>The markdown output filename.</summary>
    public const string MarkdownFilename = "Dependency Summary.md";

    /// <summary>Builds the summary markdown content for all parsed projects.</summary>
    /// <param name="solutionProjects">The projects in scope, keyed by project name.</param>
    /// <returns>The markdown summary content.</returns>
    public static string CreateContent(IDictionary<string, SolutionProject> solutionProjects)
    {
        var sb = new StringBuilder();
        var badgeProvider = new TargetFrameworkBadgeProvider();

        sb.AppendLine("# Dependency Summary");
        sb.AppendLine();

        var conflicts = GetVersionConflicts(solutionProjects);

        if (conflicts.Count > 0)
        {
            sb.AppendLine("## Cross-Project Version Conflicts");
            sb.AppendLine();

            var orderedConflicts = conflicts.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var (packageName, entries) in orderedConflicts)
            {
                sb.AppendLine($"### {packageName}");
                sb.AppendLine();
                sb.AppendLine("| Project | Resolved | Conflict Details |");
                sb.AppendLine("|---------|----------|----------------------|");

                var orderedEntries = entries
                    .OrderBy(entry => entry.Version, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.ProjectName, StringComparer.OrdinalIgnoreCase);

                foreach (var entry in orderedEntries)
                {
                    var requestedVersionPaths = GetRequestedVersionPathCell(entry.ProjectName, entries);

                    sb.AppendLine($"| {entry.ProjectName} | {entry.Version} | {requestedVersionPaths} |");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
        }

        var orderedSolutionProjects = solutionProjects.OrderBy(project => project.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var solutionProject in orderedSolutionProjects)
        {
            var project = Path.GetFileNameWithoutExtension(solutionProject.Value.Path);

            sb.AppendLine($"## {project}");
            sb.AppendLine();

            var frameworkBadges = GetTargetFrameworkBadges(solutionProject, badgeProvider);
            var projectBadges = string.Join(" ", frameworkBadges);

            sb.AppendLine(projectBadges);
            sb.AppendLine();
            sb.AppendLine("### Dependencies");
            sb.AppendLine();

            var dependencies = AppendProjectDependencies(solutionProject.Value, solutionProjects);

            if (dependencies.Length > 0)
            {
                foreach (var dependency in dependencies)
                {
                    sb.AppendLine($"* {dependency}");
                }
            }
            else
            {
                sb.AppendLine("* None");
            }

            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static Dictionary<string, ConflictEntry[]> GetVersionConflicts(IDictionary<string, SolutionProject> solutionProjects)
    {
        var packageProjectVersions = new Dictionary<string, List<ConflictEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in solutionProjects)
        {
            var projectName = kvp.Key;
            var requestedVersionPaths = GetRequestedVersionPathsByPackage(kvp.Value.PackageReferences);

            var directPackages = kvp.Value.PackageReferences
                .Where(package => package.Depth == 0)
                .Select(package => (package.Name, package.Version))
                .Distinct();

            foreach (var (name, version) in directPackages)
            {
                if (!packageProjectVersions.TryGetValue(name, out var list))
                {
                    list = [];
                    packageProjectVersions[name] = list;
                }

                string[] versionPaths = requestedVersionPaths.TryGetValue(name, out var paths)
                    ? [.. paths.Order(StringComparer.OrdinalIgnoreCase)]
                    : [];

                var conflictEntry = new ConflictEntry(projectName, version, versionPaths);

                list.Add(conflictEntry);
            }
        }

        return packageProjectVersions
            .Where(packageEntry =>
            {
                var distinctCount = packageEntry.Value
                    .Select(packageVersion => packageVersion.Version)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                return distinctCount > 1;
            })
            .ToDictionary(
                packageEntry => packageEntry.Key,
                packageEntry => packageEntry.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string GetRequestedVersionPathCell(string projectName, ConflictEntry[] entries)
    {
        var matchingEntry = entries.First(entry => string.Equals(entry.ProjectName, projectName, StringComparison.OrdinalIgnoreCase));

        return matchingEntry.RequestedVersionPaths.Length == 0
            ? $"Project resolved v{matchingEntry.Version}"
            : string.Join("<br>", matchingEntry.RequestedVersionPaths);
    }

    private static Dictionary<string, HashSet<string>> GetRequestedVersionPathsByPackage(PackageReference[] packageReferences)
    {
        var requestedPathsByPackage = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageReference in packageReferences)
        {
            var activePath = new List<string>
            {
                FormatPackagePathNode(packageReference)
            };

            CollectRequestedVersionPaths(packageReference, activePath, requestedPathsByPackage);
        }

        return requestedPathsByPackage;
    }

    private static void CollectRequestedVersionPaths(PackageReference packageReference, List<string> activePath,
        Dictionary<string, HashSet<string>> requestedPathsByPackage)
    {
        if (packageReference.RequestedDifferentVersion)
        {
            if (!requestedPathsByPackage.TryGetValue(packageReference.Name, out var paths))
            {
                paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                requestedPathsByPackage[packageReference.Name] = paths;
            }

            paths.Add(FormatRequestedVersionPath(activePath, packageReference));
        }

        foreach (var childReference in packageReference.TransitiveReferences)
        {
            activePath.Add(FormatPackagePathNode(childReference));

            CollectRequestedVersionPaths(childReference, activePath, requestedPathsByPackage);
            activePath.RemoveAt(activePath.Count - 1);
        }
    }

    private static string FormatPackagePathNode(PackageReference packageReference)
    {
        return $"{packageReference.Name} v{packageReference.Version}";
    }

    private static string FormatRequestedVersionPath(List<string> activePath, PackageReference packageReference)
    {
        return activePath.Count == 1
            ? $"Project requested {packageReference.Name} {packageReference.RequestedVersionRange}, resolved v{packageReference.Version}"
            : $"Via {string.Join(" -> ", activePath[..^1])} requested {packageReference.Name} {packageReference.RequestedVersionRange}, resolved v{packageReference.Version}";
    }

    private static List<string> GetTargetFrameworkBadges(KeyValuePair<string, SolutionProject> solutionProject, TargetFrameworkBadgeProvider badgeProvider)
    {
        var frameworkBadges = new List<string>();
        var seenBadges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var framework in solutionProject.Value.TargetFrameworks.Order(StringComparer.OrdinalIgnoreCase))
        {
            var badge = badgeProvider.GetBadge(framework);

            if (seenBadges.Add(badge))
            {
                frameworkBadges.Add(badge);
            }
        }

        return frameworkBadges;
    }

    private static string GetProjectName(ProjectReference projectReference)
    {
        return Path.GetFileNameWithoutExtension(projectReference.Path);
    }

    private static string[] AppendProjectDependencies(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects)
    {
        var dependencySet = new HashSet<string>();
        var transitiveSet = new HashSet<string>();
        var activePathProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AppendFrameworkDependencies(solutionProject, dependencySet);
        AppendPackageDependencies(solutionProject, dependencySet, transitiveSet);

        foreach (var project in solutionProject.ProjectReferences)
        {
            AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, transitiveSet, activePathProjects);
        }

        dependencySet.UnionWith(transitiveSet);

        return [.. dependencySet.Order()];
    }

    private static void AppendProjectDependenciesRecursively(ProjectReference projectReference, IDictionary<string, SolutionProject> solutionProjects,
        HashSet<string> dependencySet, HashSet<string> transitiveSet, HashSet<string> activePathProjects)
    {
        var projectName = GetProjectName(projectReference);

        if (!solutionProjects.TryGetValue(projectName, out var solutionProject))
        {
            throw new DependencyGeneratorException($"The dependency project '{projectName}' was not found using the provided regex paths.");
        }

        // Defensive check: valid project reference graphs are expected to be acyclic.
        // Throw to prevent runaway recursion if malformed or inconsistent project metadata is encountered.
        if (!activePathProjects.Add(projectName))
        {
            throw new DependencyGeneratorException($"A circular project reference was detected while building the dependency summary for '{projectName}'.");
        }

        dependencySet.Add(projectName);

        try
        {
            // Add all package dependencies (recursively) for the current project
            var packageReferences = solutionProject.PackageReferences;

            foreach (var packageReference in packageReferences)
            {
                AppendPackageDependenciesRecursively(packageReference, dependencySet, transitiveSet);
            }

            // Add all project dependencies (recursively) for the current project
            foreach (var project in solutionProject.ProjectReferences)
            {
                AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, transitiveSet, activePathProjects);
            }
        }
        finally
        {
            activePathProjects.Remove(projectName);
        }
    }

    private static void AppendFrameworkDependencies(SolutionProject solutionProject, HashSet<string> dependencySet)
    {
        foreach (var framework in solutionProject.FrameworkReferences)
        {
            dependencySet.Add(framework.Name);
        }
    }

    private static void AppendPackageDependencies(SolutionProject solutionProject, HashSet<string> dependencySet, HashSet<string> transitiveSet)
    {
        foreach (var package in solutionProject.PackageReferences)
        {
            AppendPackageDependenciesRecursively(package, dependencySet, transitiveSet);
        }
    }

    private static void AppendPackageDependenciesRecursively(PackageReference packageReference, HashSet<string> dependencySet, HashSet<string> transitiveSet)
    {
        var packageNameVersion = $"{packageReference.Name} v{packageReference.Version}";

        if (packageReference.Depth == 0)
        {
            dependencySet.Add(packageNameVersion);
        }
        else
        {
            transitiveSet.Add(packageNameVersion);
        }

        foreach (var package in packageReference.TransitiveReferences)
        {
            AppendPackageDependenciesRecursively(package, dependencySet, transitiveSet);
        }
    }
}