using AllOverIt.Extensions;
using AllOverIt.IO;
using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Parses solution projects and resolves project, framework, and package dependencies.</summary>
internal sealed class SolutionParser
{
    private readonly Dictionary<string, SolutionFile> _solutionFiles = [];
    private readonly ProjectAssetReader _assetReader = new();

    // Returns the distinct target frameworks present across all matching projects,
    // discovered from each project's project.assets.json (not from config).
    /// <summary>Discovers target frameworks from matching projects in the solution.</summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <returns>The ordered list of discovered target frameworks.</returns>
    public string[] DiscoverTargetFrameworks(string solutionFilePath, string[] regexToInclude, string[] regexToExclude)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        return FilterAndOrderProjects(solutionFilePath, regexToInclude, regexToExclude)
            .SelectMany(project => _assetReader.GetTargetFrameworks(project.AbsolutePath))
            .Select(targetFramework => targetFramework.Split('-')[0])      // strip platform suffix (e.g. net10.0-windows10.0.19041 -> net10.0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(TfmSortVersion)
            .ThenBy(targetFramework => targetFramework, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // Returns the projects that target the given framework, with their resolved packages
    // read from project.assets.json and project/framework references read from raw XML.
    /// <summary>Parses matching projects for a specific target framework.</summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <param name="excludePackages">Package IDs to exclude from package resolution.</param>
    /// <param name="targetFramework">The target framework to parse.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive package depth to include.</param>
    /// <returns>The parsed solution projects for the target framework.</returns>
    public SolutionProject[] Parse(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        string[] excludePackages, string targetFramework, int maxTransitiveDepth)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        var excludeSet = new HashSet<string>(excludePackages, StringComparer.OrdinalIgnoreCase);

        return FilterAndOrderProjects(solutionFilePath, regexToInclude, regexToExclude)
            .Where(project => _assetReader.HasTargetFramework(project.AbsolutePath, targetFramework))
            .Select(project => BuildSolutionProject(project, targetFramework, maxTransitiveDepth, excludeSet))
            .ToArray();
    }

    /// <summary>Converts a target framework moniker into a sortable version number.</summary>
    /// <param name="tfm">The target framework moniker.</param>
    /// <returns>The parsed version, or 0.0 when no version segment is found.</returns>
    private static Version TfmSortVersion(string tfm)
    {
        // e.g. "net10.0-windows" -> "10.0", "netstandard2.1" -> "2.1"
        var match = Regex.Match(tfm, @"^[a-z]+(\d+\.\d+)", RegexOptions.IgnoreCase);

        return match.Success ? Version.Parse(match.Groups[1].Value) : new Version(0, 0);
    }

    /// <summary>Filters solution projects using include/exclude regex rules and orders by project name.</summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <returns>The filtered and ordered project list.</returns>
    private IEnumerable<ProjectInSolution> FilterAndOrderProjects(
        string solutionFilePath,
        string[] regexToInclude,
        string[] regexToExclude)
    {
        if (!_solutionFiles.TryGetValue(solutionFilePath, out var solutionFile))
        {
            solutionFile = SolutionFile.Parse(solutionFilePath);
            _solutionFiles.Add(solutionFilePath, solutionFile);
        }

        var includeRegexes = regexToInclude.SelectToArray(regex => new Regex(regex));
        var excludeRegexes = regexToExclude.SelectToArray(regex => new Regex(regex));

        return solutionFile.ProjectsInOrder
            .Where(project =>
                project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                project.ProjectType == SolutionProjectType.WebProject)
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
    }

    /// <summary>Builds a <see cref="SolutionProject"/> including project, framework, and package dependencies.</summary>
    /// <param name="projectInSolution">The project entry from the solution file.</param>
    /// <param name="targetFramework">The target framework to resolve packages for.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive package depth to include.</param>
    /// <param name="excludePackages">Package IDs to exclude from package resolution.</param>
    /// <returns>The resolved solution project.</returns>
    private SolutionProject BuildSolutionProject(
        ProjectInSolution projectInSolution,
        string targetFramework,
        int maxTransitiveDepth,
        HashSet<string> excludePackages)
    {
        var projectPath = projectInSolution.AbsolutePath;
        var projectFolder = Path.GetDirectoryName(projectPath)!;

        // Project and framework references are read from the raw project XML.
        // These items (<ProjectReference>, <FrameworkReference>) are defined directly
        // in the project file and do not require MSBuild import-chain evaluation.
        var projectRootElement = ProjectRootElement.Open(projectPath);
        var projectReferences = GetProjectReferences(projectFolder, projectRootElement.ItemGroups);
        var frameworkReferences = GetFrameworkReferences(projectRootElement.ItemGroups);

        // Package references are read from the assets file — the authoritative post-restore
        // source that correctly reflects CPM, Directory.Build.props, and NuGet conflict resolution.
        var packageReferences = _assetReader.ReadPackagesForFramework(projectPath, excludePackages, targetFramework, maxTransitiveDepth);

        // All frameworks the project targets (used for badge display in the summary report).
        var allTargetFrameworks = _assetReader.GetTargetFrameworks(projectPath);

        var dependencies = new ConditionalReferences
        {
            Condition = string.Empty,
            ProjectReferences = projectReferences,
            FrameworkReferences = frameworkReferences,
            PackageReferences = packageReferences
        };

        return new SolutionProject
        {
            Name = projectInSolution.ProjectName,
            Path = projectPath,
            TargetFrameworks = allTargetFrameworks,
            Dependencies = [dependencies]
        };
    }

    /// <summary>Gets project references from raw project XML item groups.</summary>
    /// <param name="projectFolder">The base project folder used to resolve relative paths.</param>
    /// <param name="itemGroups">The project XML item groups.</param>
    /// <returns>The resolved project references.</returns>
    private static List<ProjectReference> GetProjectReferences(
        string projectFolder,
        IEnumerable<ProjectItemGroupElement> itemGroups)
    {
        return itemGroups
            .SelectMany(group => group.Items)
            .Where(item => item.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
            .SelectToList(item =>
            {
                var projectPath = FileUtils.GetAbsolutePath(projectFolder, item.Include);

                return new ProjectReference
                {
                    Path = projectPath
                };
            });
    }

    /// <summary>Gets framework references from raw project XML item groups.</summary>
    /// <param name="itemGroups">The project XML item groups.</param>
    /// <returns>The framework references.</returns>
    private static IReadOnlyCollection<FrameworkReference> GetFrameworkReferences(
        IEnumerable<ProjectItemGroupElement> itemGroups)
    {
        return itemGroups
            .SelectMany(group => group.Items)
            .Where(item => item.ItemType.Equals("FrameworkReference", StringComparison.OrdinalIgnoreCase))
            .Select(item => new FrameworkReference { Name = item.Include })
            .AsReadOnlyCollection();
    }
}
