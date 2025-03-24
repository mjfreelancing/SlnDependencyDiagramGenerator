using AllOverIt.Extensions;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnDependencyDiagramGenerator.Generator
{
    internal static class SummaryDependencyGenerator
    {
        public static readonly IDictionary<string, string> TargetFrameworkBadges = new Dictionary<string, string>
        {
            { "net9.0", $"![](https://img.shields.io/badge/.NET-9.0-{ColorCode.Yellow}.svg)"},
            { "net9.0-windows", $"![](https://img.shields.io/badge/.NET-9.0--windows-{ColorCode.Yellow}.svg)"},
            { "net8.0", $"![](https://img.shields.io/badge/.NET-8.0-{ColorCode.Purple}.svg)"},
            { "net8.0-windows", $"![](https://img.shields.io/badge/.NET-8.0--windows-{ColorCode.Purple}.svg)"},
            { "net7.0", $"![](https://img.shields.io/badge/.NET-7.0-{ColorCode.Blue}.svg)"},
            { "net7.0-windows", $"![](https://img.shields.io/badge/.NET-7.0--windows-{ColorCode.Blue}.svg)"},
            { "net6.0", $"![](https://img.shields.io/badge/.NET-6.0-{ColorCode.Orange}.svg)"},
            { "net6.0-windows", $"![](https://img.shields.io/badge/.NET-6.0--windows-{ColorCode.Orange}.svg)"},
            { "netstandard2.1", $"![](https://img.shields.io/badge/.NET-standard2.1-{ColorCode.Green}.svg)"},
            { "netstandard2.0", $"![](https://img.shields.io/badge/.NET-standard2.0-{ColorCode.Red}.svg)"}
        };

        public const string MarkdownFilename = "Dependency Summary.md";

        public static string CreateContent(IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Dependency Summary");
            sb.AppendLine();

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
                sb.AppendLine($"<br>");
                sb.AppendLine();
                sb.AppendLine($"---");
                sb.AppendLine();
                sb.AppendLine($"<br>");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static List<string> GetTargetFrameworkBadges(KeyValuePair<string, SolutionProject> solutionProject)
        {
            var frameworkBadges = new List<string>();

            foreach (var badgeKey in TargetFrameworkBadges.Keys)
            {
                foreach (var framework in solutionProject.Value.TargetFrameworks)
                {
                    // eg., compare both are net8.0
                    var exactMatch = string.Compare(badgeKey, framework, StringComparison.OrdinalIgnoreCase) == 0;

                    if (exactMatch)
                    {
                        frameworkBadges.Add(TargetFrameworkBadges[badgeKey]);
                    }
                    else
                    {
                        var badge = TargetFrameworkBadges[badgeKey];

                        // eg., compare net8.0-windows10.0.19041 with net8.0-windows, but don't add it more than once
                        //      (multiple versions of windows may be specfied)
                        if (!frameworkBadges.Contains(badge) && badgeKey.Contains("windows") && framework.StartsWith(badgeKey))
                        {
                            frameworkBadges.Add(badge);
                        }
                    }
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
            var projectName = solutionProject.Name;

            foreach (var framework in solutionProject.Dependencies.SelectMany(item => item.FrameworkReferences))
            {
                dependencySet.Add(framework.Name);
            }
        }

        private static void AppendPackageDependencies(SolutionProject solutionProject, HashSet<string> dependencySet, HashSet<string> transitiveSet)
        {
            var projectName = solutionProject.Name;

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
}