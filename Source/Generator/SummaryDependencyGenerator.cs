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
    private static readonly TargetFrameworkBadgeProvider BadgeProvider = new();

    /// <summary>The markdown output filename.</summary>
    public const string MarkdownFilename = "Dependency Summary.md";

    /// <summary>Builds the summary markdown content for all parsed projects.</summary>
    /// <param name="solutionProjects">The projects in scope, keyed by project name.</param>
    /// <returns>The markdown summary content.</returns>
    public static string CreateContent(IDictionary<string, SolutionProject> solutionProjects)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Dependency Summary");
        sb.AppendLine();

        var conflicts = GetVersionConflicts(solutionProjects);

        if (conflicts.Count > 0)
        {
            sb.AppendLine("## Version Conflicts");
            sb.AppendLine();
            sb.AppendLine("| Package | Version | Project |");
            sb.AppendLine("|---------|---------|---------|" );

            foreach (var (packageName, entries) in conflicts.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var (projectName, version) in entries.OrderBy(entry => entry.Version, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.ProjectName, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"| {packageName} | {version} | {projectName} |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("<br>");
            sb.AppendLine();
        }

        foreach (var solutionProject in solutionProjects)
        {
            var project = Path.GetFileNameWithoutExtension(solutionProject.Value.Path);
            sb.AppendLine($"## {project}");
            sb.AppendLine();

            var frameworkBadges = GetTargetFrameworkBadges(solutionProject);

            var projectBadges = string.Join(" ", frameworkBadges);
            sb.AppendLine(projectBadges);

            sb.AppendLine();

            sb.AppendLine("### Dependencies");
            sb.AppendLine();

            var dependencies = AppendProjectDependencies(solutionProject.Value, solutionProjects);

            if (dependencies.Count > 0)
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

    private static IReadOnlyDictionary<string, IReadOnlyList<(string ProjectName, string Version)>> GetVersionConflicts(
        IDictionary<string, SolutionProject> solutionProjects)
    {
        var packageProjectVersions = new Dictionary<string, List<(string ProjectName, string Version)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in solutionProjects)
        {
            var projectName = kvp.Key;

            var directPackages = kvp.Value.Dependencies
                .SelectMany(dependency => dependency.PackageReferences)
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

                list.Add((projectName, version));
            }
        }

        return packageProjectVersions
            .Where(packageEntry => packageEntry.Value.Select(packageVersion => packageVersion.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .ToDictionary(
                packageEntry => packageEntry.Key,
                packageEntry => (IReadOnlyList<(string ProjectName, string Version)>) packageEntry.Value.AsReadOnly(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> GetTargetFrameworkBadges(KeyValuePair<string, SolutionProject> solutionProject)
    {
        var frameworkBadges = new List<string>();
        var seenBadges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var framework in solutionProject.Value.TargetFrameworks)
        {
            var badge = BadgeProvider.GetBadge(framework);

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

    private static IReadOnlyCollection<string> AppendProjectDependencies(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects)
    {
        var dependencySet = new HashSet<string>();
        var transitiveSet = new HashSet<string>();

        AppendFrameworkDependencies(solutionProject, dependencySet);
        AppendPackageDependencies(solutionProject, dependencySet, transitiveSet);

        foreach (var project in solutionProject.Dependencies.SelectMany(item => item.ProjectReferences))
        {
            AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, transitiveSet);
        }

        dependencySet.UnionWith(transitiveSet);

        return dependencySet
            .Order()
            .AsReadOnlyCollection();
    }

    private static void AppendProjectDependenciesRecursively(ProjectReference projectReference, IDictionary<string, SolutionProject> solutionProjects,
        HashSet<string> dependencySet, HashSet<string> transitiveSet)
    {
        var projectName = GetProjectName(projectReference);

        if (!solutionProjects.TryGetValue(projectName, out var solutionProject))
        {
            throw new DependencyGeneratorException($"The dependency project '{projectName}' was not found using the provided regex paths.");
        }

        dependencySet.Add(projectName);

        // Add all packages dependencies (recursively) for the current project
        var packageReferences = solutionProject.Dependencies.SelectMany(item => item.PackageReferences);

        foreach (var packageReference in packageReferences)
        {
            AppendPackageDependenciesRecursively(packageReference, dependencySet, transitiveSet);
        }

        // Add all project dependencies (recursively) for the current project
        foreach (var project in solutionProjects[projectName].Dependencies.SelectMany(item => item.ProjectReferences))
        {
            AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, transitiveSet);
        }
    }

    private static void AppendFrameworkDependencies(SolutionProject solutionProject, HashSet<string> dependencySet)
    {
        foreach (var framework in solutionProject.Dependencies.SelectMany(item => item.FrameworkReferences))
        {
            dependencySet.Add(framework.Name);
        }
    }

    private static void AppendPackageDependencies(SolutionProject solutionProject, HashSet<string> dependencySet, HashSet<string> transitiveSet)
    {
        foreach (var package in solutionProject.Dependencies.SelectMany(item => item.PackageReferences))
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