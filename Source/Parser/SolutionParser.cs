using AllOverIt.Extensions;
using Microsoft.Build.Evaluation;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Parses solution projects and resolves project, framework, and package dependencies.</summary>
internal sealed partial class SolutionParser
{
    [GeneratedRegex(@"^[a-z]+(\d+\.\d+)", RegexOptions.IgnoreCase, "en-AU")]
    private static partial Regex TargetFrameworkRegex();

    private bool _hasCachedProjects;
    private string _cachedSolutionFilePath = string.Empty;
    private IReadOnlyList<SolutionProjectDescriptor> _cachedProjects = [];
    private readonly Dictionary<string, ISolutionProjectResolver> _solutionProjectResolvers;
    private readonly ProjectAssetReader _assetReader = new();

    /// <summary>Initializes a new parser instance.</summary>
    public SolutionParser()
    {
        _solutionProjectResolvers = new Dictionary<string, ISolutionProjectResolver>(StringComparer.OrdinalIgnoreCase)
        {
            [".sln"] = new SlnSolutionProjectResolver(),
            [".slnx"] = new SlnxSolutionProjectResolver()
        };
    }

    /// <summary>
    /// Discovers the set of target frameworks that should be processed for the selected projects.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <returns>
    /// A distinct, ordered list of base target frameworks (for example, <c>net10.0</c>),
    /// discovered from each matching project's assets file.
    /// </returns>
    public async Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        var projects = await FilterAndOrderProjectsAsync(solutionFilePath, regexToInclude, regexToExclude).ConfigureAwait(false);

        return [.. projects
            .SelectMany(project => _assetReader.GetTargetFrameworks(project.AbsolutePath))
            .Select(targetFramework => targetFramework.Split('-')[0])      // strip platform suffix (e.g. net10.0-windows10.0.19041 -> net10.0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(TfmSortVersion)
            .ThenBy(targetFramework => targetFramework, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Parses all matching projects that include the requested target framework and resolves
    /// their project, framework, and package dependencies.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <param name="excludePackages">Package IDs to exclude from package resolution.</param>
    /// <param name="excludeFrameworks">Framework reference IDs to exclude from framework resolution.</param>
    /// <param name="targetFramework">The target framework to parse.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive package depth to include.</param>
    /// <returns>
    /// The parsed project models for the requested target framework.
    /// </returns>
    public async Task<SolutionProject[]> ParseAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        string[] excludePackages, string[] excludeFrameworks, string targetFramework, int maxTransitiveDepth)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        // SDK-style project evaluation requires a registered MSBuild instance so SDK resolvers
        // can locate Microsoft.NET.Sdk and related toolset components.
        MsBuildSdkResolver.EnsureInitialized();

        var excludeSet = new HashSet<string>(excludePackages, StringComparer.OrdinalIgnoreCase);
        var excludeFrameworkSet = new HashSet<string>(excludeFrameworks, StringComparer.OrdinalIgnoreCase);

        var projects = await FilterAndOrderProjectsAsync(solutionFilePath, regexToInclude, regexToExclude).ConfigureAwait(false);

        return [.. projects
            .Where(project => _assetReader.HasTargetFramework(project.AbsolutePath, targetFramework))
            .Select(project => BuildSolutionProject(project, targetFramework, maxTransitiveDepth, excludeSet, excludeFrameworkSet))];
    }

    /// <summary>
    /// Converts a target framework moniker into a sortable <see cref="Version"/>.
    /// </summary>
    /// <param name="tfm">The target framework moniker.</param>
    /// <returns>
    /// The parsed framework version, or <c>0.0</c> when no version segment is present.
    /// </returns>
    private static Version TfmSortVersion(string tfm)
    {
        // e.g. "net10.0-windows" -> "10.0", "netstandard2.1" -> "2.1"
        var match = TargetFrameworkRegex().Match(tfm);

        return match.Success
            ? Version.Parse(match.Groups[1].Value)
            : new Version(0, 0);
    }

    /// <summary>
    /// Loads and filters solution projects using include/exclude regex rules, then orders by project name.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <returns>
    /// The filtered and alphabetically ordered set of MSBuild-format projects.
    /// </returns>
    private async Task<IReadOnlyList<SolutionProjectDescriptor>> FilterAndOrderProjectsAsync(string solutionFilePath,
        string[] regexToInclude, string[] regexToExclude)
    {
        var solutionProjects = await GetSolutionProjectsAsync(solutionFilePath).ConfigureAwait(false);

        var includeRegexes = regexToInclude.SelectToArray(regex => new Regex(regex));
        var excludeRegexes = regexToExclude.SelectToArray(regex => new Regex(regex));

        return [.. solutionProjects
            .Where(project =>
            {
                var include = includeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);

                if (!include || excludeRegexes.Length == 0)
                {
                    return include;
                }

                return !excludeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);
            })
            .OrderBy(item => item.ProjectName)];
    }

    /// <summary>
    /// Loads project entries from a supported solution format.
    /// </summary>
    /// <param name="solutionFilePath">The full solution file path.</param>
    /// <returns>The project entries available in the solution.</returns>
    /// <exception cref="DependencyGeneratorException">Thrown when the extension is unsupported or the solution cannot be parsed.</exception>
    private async Task<IReadOnlyList<SolutionProjectDescriptor>> GetSolutionProjectsAsync(string solutionFilePath)
    {
        if (_hasCachedProjects && string.Equals(_cachedSolutionFilePath, solutionFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return _cachedProjects;
        }

        var extension = Path.GetExtension(solutionFilePath);

        if (!_solutionProjectResolvers.TryGetValue(extension, out var resolver))
        {
            var supportedExtensions = string.Join(", ", _solutionProjectResolvers.Keys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase));
            throw new DependencyGeneratorException($"Unsupported solution extension '{extension}'. Supported extensions are {supportedExtensions}. Path: {solutionFilePath}");
        }

        IReadOnlyList<SolutionProjectDescriptor> solutionProjects;

        try
        {
            solutionProjects = await resolver.GetProjectsAsync(solutionFilePath).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw CreateSolutionParseException(solutionFilePath, exception);
        }

        _cachedSolutionFilePath = solutionFilePath;
        _cachedProjects = solutionProjects;
        _hasCachedProjects = true;

        return solutionProjects;
    }
    /// <summary>
    /// Builds a <see cref="SolutionProject"/> by combining evaluated MSBuild references and
    /// assets-resolved package dependencies for one target framework.
    /// </summary>
    /// <param name="solutionProject">The project entry from the solution file.</param>
    /// <param name="targetFramework">The target framework to resolve packages for.</param>
    /// <param name="maxTransitiveDepth">The maximum transitive package depth to include.</param>
    /// <param name="excludePackages">Package IDs to exclude from package resolution.</param>
    /// <param name="excludeFrameworks">Framework reference IDs to exclude from framework resolution.</param>
    /// <returns>
    /// The resolved project model used by downstream diagram and summary generation.
    /// </returns>
    private SolutionProject BuildSolutionProject(SolutionProjectDescriptor solutionProject, string targetFramework,
        int maxTransitiveDepth, HashSet<string> excludePackages, HashSet<string> excludeFrameworks)
    {
        var projectPath = solutionProject.AbsolutePath;

        // Evaluate the project for the current target framework so imported/conditioned items
        // from Directory.Build.props/targets are included in ProjectReference and FrameworkReference.
        using var projectCollection = new ProjectCollection(new Dictionary<string, string>
        {
            ["TargetFramework"] = targetFramework
        });

        Project evaluatedProject;

        try
        {
            evaluatedProject = projectCollection.LoadProject(projectPath);
        }
        catch (Exception exception)
        {
            throw CreateMsBuildEvaluationException(projectPath, targetFramework, exception);
        }

        ProjectReference[] projectReferences;
        FrameworkReference[] frameworkReferences;

        try
        {
            projectReferences = GetProjectReferences(evaluatedProject);
            frameworkReferences = GetFrameworkReferences(evaluatedProject, excludeFrameworks);
        }
        catch (Exception exception)
        {
            throw CreateMsBuildEvaluationException(projectPath, targetFramework, exception);
        }

        // Package references are read from the assets file — the authoritative post-restore
        // source that correctly reflects CPM, Directory.Build.props, and NuGet conflict resolution.
        var packageReferences = _assetReader.ReadPackagesForFramework(projectPath, excludePackages, targetFramework, maxTransitiveDepth);

        // All frameworks the project targets (used for badge display in the summary report).
        var allTargetFrameworks = _assetReader.GetTargetFrameworks(projectPath);

        return new SolutionProject
        {
            Name = solutionProject.ProjectName,
            Path = projectPath,
            TargetFrameworks = allTargetFrameworks,
            ProjectReferences = projectReferences,
            FrameworkReferences = frameworkReferences,
            PackageReferences = packageReferences
        };
    }

    /// <summary>
    /// Creates a diagnostic-rich parser exception for malformed or unreadable solution files.
    /// </summary>
    /// <param name="solutionFilePath">The solution path being parsed when the failure occurred.</param>
    /// <param name="exception">The underlying exception thrown while parsing the solution file.</param>
    /// <returns>A <see cref="DependencyGeneratorException"/> that includes solution path and underlying parser diagnostics.</returns>
    private static DependencyGeneratorException CreateSolutionParseException(string solutionFilePath, Exception exception)
    {
        var extension = Path.GetExtension(solutionFilePath);
        var builder = new StringBuilder();

        builder.AppendLine($"Failed to parse solution file '{solutionFilePath}'.");
        builder.AppendLine($"Detected format: {extension}");
        builder.AppendLine();
        builder.AppendLine("Underlying exception:");
        builder.AppendLine(exception.ToString());

        return new DependencyGeneratorException(builder.ToString(), exception);
    }

    /// <summary>
    /// Extracts <c>ProjectReference</c> items from an evaluated project and normalizes them to absolute paths.
    /// </summary>
    /// <param name="project">The evaluated project for the active target framework.</param>
    /// <returns>
    /// The resolved project references for the active target framework.
    /// </returns>
    private static ProjectReference[] GetProjectReferences(Project project)
    {
        return [.. project.Items
            .Where(item => item.ItemType.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(item =>
            {
                var projectPath = item.GetMetadataValue("FullPath");

                return new ProjectReference
                {
                    Path = Path.GetFullPath(projectPath)
                };
            })];
    }

    /// <summary>
    /// Extracts <c>FrameworkReference</c> items from an evaluated project.
    /// </summary>
    /// <param name="project">The evaluated project for the active target framework.</param>
    /// <param name="excludeFrameworks">Framework reference IDs to exclude.</param>
    /// <returns>
    /// The framework references for the active target framework.
    /// </returns>
    private static FrameworkReference[] GetFrameworkReferences(Project project, HashSet<string> excludeFrameworks)
    {
        return [.. project.Items
            .Where(item => item.ItemType.Equals("FrameworkReference", StringComparison.OrdinalIgnoreCase))
            .Where(item => !excludeFrameworks.Contains(item.EvaluatedInclude))
            .Select(item => new FrameworkReference { Name = item.EvaluatedInclude })];
    }

    /// <summary>
    /// Creates a diagnostic-rich parser exception for MSBuild evaluation failures.
    /// </summary>
    /// <param name="projectPath">The project path being evaluated when the failure occurred.</param>
    /// <param name="targetFramework">The target framework being evaluated when the failure occurred.</param>
    /// <param name="exception">The underlying exception thrown by MSBuild evaluation or item processing.</param>
    /// <returns>
    /// A <see cref="DependencyGeneratorException"/> that includes project context, target framework,
    /// resolver diagnostics, and the full underlying exception details.
    /// </returns>
    private static DependencyGeneratorException CreateMsBuildEvaluationException(string projectPath, string targetFramework, Exception exception)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Failed while evaluating an SDK-style project with MSBuild.");
        builder.AppendLine($"Project path: {projectPath}");
        builder.AppendLine($"Target framework: {targetFramework}");
        builder.AppendLine();
        builder.AppendLine("MSBuild diagnostic context:");
        builder.AppendLine(MsBuildSdkResolver.GetDiagnostics());
        builder.AppendLine("Underlying exception:");
        builder.AppendLine(exception.ToString());

        return new DependencyGeneratorException(builder.ToString(), exception);
    }
}
