using AllOverIt.Extensions;
using AllOverIt.IO;
using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram.Parser
{
    internal sealed class SolutionParser
    {
        private static readonly Regex TargetFrameworksRegex = new(@"'\$\(TargetFramework\)'\s*==\s*'(?<target>.*?)'", RegexOptions.Singleline);

        private readonly NugetPackageReferencesResolver _nugetResolver;

        public SolutionParser(int maxTransitiveDepth)
        {
            _nugetResolver = new NugetPackageReferencesResolver(maxTransitiveDepth);
        }

        public async Task<IReadOnlyCollection<SolutionProject>> ParseAsync(string solutionFilePath, string projectPathRegex, string targetFramework)
        {
            var projects = new List<SolutionProject>();

            var solutionFile = SolutionFile.Parse(solutionFilePath);

            var regex = new Regex(projectPathRegex);

            var orderedProjects = solutionFile.ProjectsInOrder
                .Where(project => project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                .Where(project =>
                {
                    return regex.Matches(project.AbsolutePath).Count > 0;
                })
                .OrderBy(item => item.ProjectName);

            foreach (var projectItem in orderedProjects)
            {
                var projectRootElement = ProjectRootElement.Open(projectItem.AbsolutePath);
                var projectFolder = Path.GetDirectoryName(projectItem.AbsolutePath);

                var targetFrameworks = GetTargetFrameworks(projectRootElement.PropertyGroups);

                // Can't skip (without additional logic) as we need to cater for WPF projects targeting, such as net7.0-windows;net6.0-windows
                //
                // if (!targetFrameworks.Contains(targetFramework))
                // {
                //     continue;
                // }

                var conditionalReferences = await GetConditionalReferencesAsync(projectFolder, projectRootElement.ItemGroups, targetFramework).ToListAsync();

                var project = new SolutionProject
                {
                    Name = projectItem.ProjectName,
                    Path = projectItem.AbsolutePath,
                    TargetFrameworks = targetFrameworks,
                    Dependencies = conditionalReferences.AsReadOnlyCollection()
                };

                projects.Add(project);
            }

            return projects;
        }

        private static IReadOnlyCollection<string> GetTargetFrameworks(IEnumerable<ProjectPropertyGroupElement> propertyGroups)
        {
            return propertyGroups
                .SelectMany(grp => grp.Properties)
                .Where(prop => prop.Name.Equals("TargetFrameworks", StringComparison.OrdinalIgnoreCase) ||
                               prop.Name.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase))
                .Select(prop => prop.Value)
                .Single()
                .Split(";");
        }

        private async IAsyncEnumerable<ConditionalReferences> GetConditionalReferencesAsync(string projectFolder, IEnumerable<ProjectItemGroupElement> itemGroups,
            string targetFramework)
        {
            var conditionItemGroups = itemGroups
                .Select(grp => new
                {
                    grp.Condition,
                    grp.Items
                })
                .GroupBy(grp => grp.Condition);

            foreach (var itemGroup in conditionItemGroups)
            {
                // Should more elaborate parsing be required, refer to this link for possible condition usage:
                // https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-conditions?view=vs-2022

                // Example: '$(TargetFramework)' == 'netstandard2.1' or '$(TargetFramework)' == 'net5.0'
                var condition = itemGroup.Key;

                // Only process conditions that have an exact match (when not empty)
                if (!condition.IsNullOrEmpty())
                {
                    // Matches a string that starts with '$(TargetFramework)', followed by zero or more whitespace characters, followed by '==',
                    // followed by zero or more whitespace characters, followed by a string enclosed in single quotes. The contents of the string
                    // inside the single quotes are captured in a named group called 'target'.
                    //
                    // (?   - This starts a named capture group. The <target> part of the group indicates the name of the group.
                    //        The ? indicates that this is an optional group.
                    //
                    // .*?  - This matches any character (except for a newline if 'RegexOptions.Singleline' is not used) zero or more times,
                    //        but as few times as possible (a non-greedy match).
                    var matches = TargetFrameworksRegex.Matches(condition).Select(item => item.Groups["target"].Value);

                    if (!matches.Contains(targetFramework))
                    {
                        continue;
                    }
                }

                var items = itemGroup.SelectMany(value => value.Items).ToList();

                var projectReferences = GetProjectReferences(projectFolder, items);
                var packageReferences = await GetPackageReferencesAsync(items, targetFramework);

                var conditionalReferences = new ConditionalReferences
                {
                    Condition = condition,
                    ProjectReferences = projectReferences,
                    PackageReferences = packageReferences
                };

                yield return conditionalReferences;
            }
        }

        private static IReadOnlyCollection<ProjectReference> GetProjectReferences(string projectFolder, IEnumerable<ProjectItemElement> projectItems)
        {
            return projectItems
                .Where(item => item.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
                .Select(item =>
                {
                    var projectPath = FileUtils.GetAbsolutePath(projectFolder, item.Include);

                    return new ProjectReference
                    {
                        Path = projectPath
                    };
                })
                .ToList();
        }

        private async Task<IReadOnlyCollection<PackageReference>> GetPackageReferencesAsync(IEnumerable<ProjectItemElement> projectItems, string targetFramework)
        {
            var packageReferences = await projectItems
                .Where(item => item.ItemType.Equals("PackageReference", StringComparison.OrdinalIgnoreCase))
                .SelectAsync(async item =>
                {
                    var packageName = item.Include;

                    var packageVersion = item.Metadata.SingleOrDefault(item => item.Name == "Version")?.Value;

                    var transitivePackages = await _nugetResolver.GetPackageReferences(packageName, packageVersion, targetFramework);

                    return new PackageReference
                    {
                        Name = packageName,
                        Version = packageVersion,
                        TransitiveReferences = transitivePackages
                    };
                })
                .ToListAsync();

            return packageReferences.AsReadOnlyCollection();
        }
    }
}