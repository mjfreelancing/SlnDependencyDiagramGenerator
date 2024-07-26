using AllOverIt.Extensions;
using AllOverIt.IO;
using AllOverIt.Logging;
using Microsoft.Build.Construction;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram.Parser
{
    internal sealed partial class SolutionParser
    {
        /*
            '\\$\\(TargetFramework\\)'  : This part matches the literal string $(TargetFramework).

            '\\s*'                      : This matches zero or more whitespace characters (such as spaces, tabs, or line breaks).

            '(?<operator>[!=]=)'        : Captures either != or == in the operator named group. The [!=] part specifies that only
                                         ! or = is allowed before the =.

            '\\s*'                      : Similar to the previous \s*, this matches zero or more whitespace characters.

            '(?<target>.*?)'            : This uses another named group target to capture any character (.) zero or more times (*?) in a
                                          non-greedy way, meaning it captures as few characters as possible until the next part of the
                                          pattern is matched. The ? makes the * quantifier non-greedy.

            When using the RegexOptions.Singleline option in C#, it changes the behavior of the dot (.) metacharacter to match any character,
            including newline characters (\n). By default, the dot matches any character except newline. With RegexOptions.Singleline, it will
            match newline characters as well.
        */
        [GeneratedRegex("'\\$\\(TargetFramework\\)'\\s*(?<operator>[!=]=)\\s*'(?<target>.*?)'", RegexOptions.Singleline)]
        private static partial Regex TargetFrameworkEqualityRegex();

        private readonly Dictionary<string, SolutionFile> _solutionFiles = [];

        private readonly NugetPackageResolver _nugetResolver;

        public SolutionParser(IEnumerable<NugetPackageFeed> packageFeeds, int maxTransitiveDepth, IColorConsoleLogger logger)
        {
            _nugetResolver = new NugetPackageResolver(packageFeeds, maxTransitiveDepth, logger);
        }

        public async Task<SolutionProject[]> ParseAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude, string targetFramework)
        {
            var projects = new List<SolutionProject>();

            // Make sure a rooted path is used (converts a relative path to an explicit path if required)
            solutionFilePath = Path.GetFullPath(solutionFilePath);

            if (!_solutionFiles.TryGetValue(solutionFilePath, out var solutionFile))
            {
                solutionFile = SolutionFile.Parse(solutionFilePath);

                _solutionFiles.Add(solutionFilePath, solutionFile);
            }

            var includeRegexes = regexToInclude.SelectToArray(regex => new Regex(regex));
            var excludeRegexes = regexToExclude.SelectToArray(regex => new Regex(regex));

            var orderedProjects = solutionFile.ProjectsInOrder
                .Where(project => project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat || project.ProjectType == SolutionProjectType.WebProject)
                .Where(project =>
                {
                    var include = includeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);

                    if (!include || excludeRegexes.Length == 0)
                    {
                        return include;
                    }

                    return !excludeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);
                })
                .OrderBy(item => item.ProjectName);

            foreach (var projectItem in orderedProjects)
            {
                var projectRootElement = ProjectRootElement.Open(projectItem.AbsolutePath);
                var projectFolder = Path.GetDirectoryName(projectItem.AbsolutePath);

                var targetFrameworks = GetTargetFrameworks(projectRootElement.PropertyGroups);

                if (targetFrameworks.Length == 0)
                {
                    throw new DependencyGeneratorException($"{projectRootElement.FullPath} does not specify a target framework. Importing of Directory.Build.Props is not supported.");
                }

                // Looking this way so we can detect project types, such as WPF, that may target as net8.0-windows;net7.0-windows
                if (!targetFrameworks.Any(framework => framework.Contains(targetFramework)))
                {
                    continue;
                }

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

            return [.. projects];
        }

        private static string[] GetTargetFrameworks(IEnumerable<ProjectPropertyGroupElement> propertyGroups)
        {
            var frameworks = propertyGroups
                .SelectMany(grp => grp.Properties)
                .Where(prop => prop.Name.Equals("TargetFrameworks", StringComparison.OrdinalIgnoreCase) ||
                               prop.Name.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase))
                .Select(prop => prop.Value)
                .SingleOrDefault();

            return frameworks is null
                ? []
                : frameworks.Split(";");
        }

        private async IAsyncEnumerable<ConditionalReferences> GetConditionalReferencesAsync(string projectFolder, IEnumerable<ProjectItemGroupElement> itemGroups,
            string targetFramework)
        {
            var conditionItemGroups = itemGroups
                .Where(grp => grp.Condition.IsNotNullOrEmpty())
                .Select(grp => new
                {
                    grp.Condition,
                    grp.Items
                })
                .GroupBy(grp => grp.Condition);

            foreach (var itemGroup in conditionItemGroups)
            {
                // Example: '$(TargetFramework)' == 'net8.0' or '$(TargetFramework)' == 'net7.0'
                var condition = itemGroup.Key;

                if (!condition.IsNullOrEmpty())
                {
                    var matches = TargetFrameworkEqualityRegex().Matches(condition);
                    var targets = matches.SelectToReadOnlyCollection(item => item.Groups["target"].Value);
                    var comparisons = matches.SelectToReadOnlyCollection(item => item.Groups["operator"].Value);

                    var foundMatch = false;

                    if (targets.Count != 0)
                    {
                        var combined = targets.Zip(comparisons, (target, comparison) => (target, comparison));

                        foreach (var (target, comparison) in combined)
                        {
                            // Only currently catering for single conditions (or multiple that are OR'd) that use == or !=
                            //
                            // Should more elaborate parsing be required, refer to this link for possible condition usage:
                            // https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-conditions?view=vs-2022
                            //
                            if (target.Equals(targetFramework, StringComparison.InvariantCultureIgnoreCase))
                            {
                                foundMatch = comparison.Equals("==");
                            }
                            else
                            {
                                foundMatch = comparison.Equals("!=");
                            }

                            if (foundMatch)
                            {
                                break;
                            }
                        }
                    }

                    if (!foundMatch)
                    {
                        continue;
                    }
                }

                var items = itemGroup.SelectMany(value => value.Items).ToList();

                var projectReferences = GetProjectReferences(projectFolder, items);
                var frameworkReferences = GetFrameworkReferences(items);
                var packageReferences = await GetPackageReferencesAsync(items, targetFramework);

                var conditionalReferences = new ConditionalReferences
                {
                    Condition = condition,
                    ProjectReferences = projectReferences,
                    FrameworkReferences = frameworkReferences,
                    PackageReferences = packageReferences
                };

                yield return conditionalReferences;
            }
        }

        private static List<ProjectReference> GetProjectReferences(string projectFolder, IEnumerable<ProjectItemElement> projectItems)
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

        private static IReadOnlyCollection<FrameworkReference> GetFrameworkReferences(IEnumerable<ProjectItemElement> projectItems)
        {
            return projectItems
                .Where(item => item.ItemType.Equals("FrameworkReference", StringComparison.OrdinalIgnoreCase))
                .Select(item => new FrameworkReference
                {
                    Name = item.Include
                })
                .AsReadOnlyCollection();
        }

        private async Task<IReadOnlyCollection<PackageReference>> GetPackageReferencesAsync(IEnumerable<ProjectItemElement> projectItems, string targetFramework)
        {
            var packageReferences = await projectItems
                .Where(item => item.ItemType.Equals("PackageReference", StringComparison.OrdinalIgnoreCase))
                .SelectAsync(async (item, _) =>
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