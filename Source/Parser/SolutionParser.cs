using AllOverIt.Extensions;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Parses solution projects and resolves project, framework, and package dependencies.</summary>
internal sealed partial class SolutionParser : ISolutionParser
{
    // Bounds how long any single user-supplied regex match may run. .NET's engine is backtracking-based,
    // so patterns like nested quantifiers can exhibit exponential backtracking on longer inputs — a latent
    // ReDoS risk when config regexes meet long file paths. The timeout turns a potential hang into a
    // RegexMatchTimeoutException, surfaced as a clear error. Kept in sync with the validator's 100ms bound.
    private const int RegexMatchTimeoutMilliseconds = 100;

    [GeneratedRegex(@"^[a-z]+(\d+\.\d+)", RegexOptions.IgnoreCase, "en-AU")]
    private static partial Regex TargetFrameworkRegex();

    private bool _hasCachedProjects;
    private string _cachedSolutionFilePath = string.Empty;
    private IReadOnlyList<SolutionProjectDescriptor> _cachedProjects = [];
    private readonly Dictionary<string, ISolutionProjectResolver> _solutionProjectResolvers;
    private readonly IProjectAssetReader _assetReader;
    private readonly ILogger<SolutionParser> _logger;

    /// <summary>Initializes a new parser instance.</summary>
    /// <param name="assetReader">The project assets reader used to resolve target frameworks and packages.</param>
    /// <param name="solutionProjectResolvers">The set of solution project resolvers, keyed by file extension.</param>
    /// <param name="logger">The logger used for diagnostics.</param>
    public SolutionParser(IProjectAssetReader assetReader, IEnumerable<ISolutionProjectResolver> solutionProjectResolvers,
        ILogger<SolutionParser> logger)
    {
        _assetReader = assetReader;
        _logger = logger;

        // SDK-style project evaluation requires a registered MSBuild instance so SDK resolvers
        // can locate Microsoft.NET.Sdk and related toolset components.
        MsBuildSdkResolver.EnsureInitialized();

        _logger.LogDebug("MSBuild SDK resolver initialised");

        _solutionProjectResolvers = new Dictionary<string, ISolutionProjectResolver>(StringComparer.OrdinalIgnoreCase);

        foreach (var resolver in solutionProjectResolvers)
        {
            _solutionProjectResolvers[resolver.Extension] = resolver;
        }
    }

    /// <summary>
    /// Discovers the set of target frameworks that should be processed for the selected projects.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A distinct, ordered list of base target frameworks (for example, <c>net10.0</c>),
    /// discovered from each matching project's assets file.
    /// </returns>
    public async Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        var projects = await FilterAndOrderProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken).ConfigureAwait(false);

        return await DiscoverTargetFrameworksAsync(projects, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers target frameworks for an already filtered set of projects.
    /// </summary>
    /// <param name="projects">The pre-filtered projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <inheritdoc />
    public Task<string[]> DiscoverTargetFrameworksAsync(IReadOnlyList<SolutionProjectDescriptor> projects,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Discovering target frameworks across {ProjectCount} project(s)", projects.Count);

        string[] targetFrameworks = [.. projects
            .SelectMany(project => _assetReader.GetTargetFrameworks(project.AbsolutePath))
            .Select(targetFramework => targetFramework.Split('-')[0])      // strip platform suffix (e.g. net10.0-windows10.0.19041 -> net10.0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(TfmSortVersion)
            .ThenBy(targetFramework => targetFramework, StringComparer.OrdinalIgnoreCase)];

        return Task.FromResult(targetFrameworks);
    }

    /// <summary>
    /// Parses all matching projects that include the requested target framework and resolves
    /// their project, framework, and package dependencies.
    /// </summary>
    /// <param name="request">The parse request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The parsed project models for the requested target framework.
    /// </returns>
    public async Task<SolutionProject[]> ParseAsync(SolutionParseRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var solutionFilePath = Path.GetFullPath(request.SolutionFilePath);

        var projects = await FilterAndOrderProjectsAsync(solutionFilePath, request.RegexToInclude, request.RegexToExclude, cancellationToken).ConfigureAwait(false);

        return BuildParsedProjects(request, projects);
    }

    /// <summary>
    /// Builds parsed project models from an already filtered set for the requested target framework.
    /// </summary>
    /// <param name="request">The parse request parameters.</param>
    /// <param name="projects">The pre-filtered projects.</param>
    /// <inheritdoc />
    public SolutionProject[] BuildParsedProjects(SolutionParseRequest request, IReadOnlyList<SolutionProjectDescriptor> projects)
    {
        var excludeSet = new HashSet<string>(request.ExcludePackages, StringComparer.OrdinalIgnoreCase);
        var excludeFrameworkSet = new HashSet<string>(request.ExcludeFrameworks, StringComparer.OrdinalIgnoreCase);

        var parsedProjects = (SolutionProject[])[.. projects
            .Where(project => _assetReader.HasTargetFramework(project.AbsolutePath, request.TargetFramework))
            .Select(project => BuildSolutionProject(project, request.TargetFramework, request.MaxTransitiveDepth, excludeSet, excludeFrameworkSet))
            .OrderBy(project => project.Name)];

        _logger.LogDebug("Built {ProjectCount} parsed project(s) for {TargetFramework}", parsedProjects.Length, request.TargetFramework);

        return parsedProjects;
    }

    /// <summary>
    /// Discovers and classifies all projects for the supplied regex filters.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <inheritdoc />
    public async Task<FilteredSolutionProjects> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken)
    {
        solutionFilePath = Path.GetFullPath(solutionFilePath);

        // Resolve all solution projects once so include/exclude evaluation runs against the same snapshot.
        var solutionProjects = await GetSolutionProjectsAsync(solutionFilePath, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Discovered {ProjectCount} project(s) from {SolutionPath}",
            solutionProjects.Count, Path.GetFileName(solutionFilePath));

        // Relative-path matching is performed from the solution directory because solution entries are typically relative.
        var solutionDirectory = Path.GetDirectoryName(solutionFilePath) ?? string.Empty;

        // Compile each configured pattern once up-front to avoid per-project regex construction.
        // A bounded match timeout means a pathological pattern fails fast rather than hanging generation.
        var includeRegexes = regexToInclude.SelectToArray(CompileRegex);
        var excludeRegexes = regexToExclude.SelectToArray(CompileRegex);

        try
        {
            return ClassifyProjects(solutionProjects, solutionDirectory, includeRegexes, excludeRegexes);
        }
        catch (RegexMatchTimeoutException exception)
        {
            throw new DependencyGeneratorException(
                $"The regular expression '{exception.Pattern}' exceeded the {RegexMatchTimeoutMilliseconds}ms match timeout while evaluating '{exception.Input}'. Simplify the pattern or shorten the matched path.",
                exception);
        }
    }

    /// <summary>
    /// Converts a target framework moniker into a sortable <see cref="Version"/>.
    /// </summary>
    /// <param name="targetFrameworkMoniker">The target framework moniker.</param>
    /// <returns>
    /// The parsed framework version, or <c>0.0</c> when no version segment is present.
    /// </returns>
    private static Version TfmSortVersion(string targetFrameworkMoniker)
    {
        // e.g. "net10.0-windows" -> "10.0", "netstandard2.1" -> "2.1"
        var match = TargetFrameworkRegex().Match(targetFrameworkMoniker);

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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The filtered and alphabetically ordered set of MSBuild-format projects.
    /// </returns>
    private async Task<IReadOnlyList<SolutionProjectDescriptor>> FilterAndOrderProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken)
    {
        var filteredProjects = await DiscoverProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken).ConfigureAwait(false);

        return [.. filteredProjects.IncludedProjects
            // Deterministic ordering keeps output stable across runs and simplifies testing.
            .OrderBy(item => item.ProjectName)];
    }

    private static FilteredSolutionProjects ClassifyProjects(IReadOnlyList<SolutionProjectDescriptor> solutionProjects, string solutionDirectory,
        Regex[] includeRegexes, Regex[] excludeRegexes)
    {
        var included = new List<SolutionProjectDescriptor>();
        var excluded = new List<SolutionProjectDescriptor>();
        var implicitlyExcluded = new List<SolutionProjectDescriptor>();

        foreach (var project in solutionProjects)
        {
            // Include is the first gate: if no include pattern matches, the project is rejected immediately.
            var include = IsMatch(includeRegexes, solutionDirectory, project);

            if (!include)
            {
                implicitlyExcluded.Add(project);
                continue;
            }

            // Exclude is applied only after include succeeds; any exclude match removes the project.
            var isExcluded = excludeRegexes.Length > 0 && IsMatch(excludeRegexes, solutionDirectory, project);

            if (isExcluded)
            {
                excluded.Add(project);
            }
            else
            {
                included.Add(project);
            }
        }

        return new FilteredSolutionProjects
        {
            AllProjects = [.. solutionProjects],
            IncludedProjects = [.. included],
            ExcludedProjects = [.. excluded],
            ImplicitlyExcludedProjects = [.. implicitlyExcluded]
        };
    }

    /// <summary>Compiles a user-supplied regex pattern with a bounded match timeout.</summary>
    /// <param name="pattern">The regex pattern.</param>
    /// <returns>The compiled regex.</returns>
    /// <exception cref="ArgumentException">Thrown when the pattern is not a valid regular expression.</exception>
    private static Regex CompileRegex(string pattern)
    {
        // A match timeout (rather than RegexOptions.NonBacktracking) is used because NonBacktracking rejects
        // otherwise-valid user patterns that use lookarounds, backreferences, or atomic groups. The timeout
        // bounds every match, so a pathological pattern fails fast (RegexMatchTimeoutException) instead of hanging.
        return new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(RegexMatchTimeoutMilliseconds));
    }

    private static bool IsMatch(Regex[] regexes, string solutionDirectory, SolutionProjectDescriptor project)
    {
        var absolutePath = project.AbsolutePath;
        var relativePath = Path.GetRelativePath(solutionDirectory, absolutePath);
        var fileName = Path.GetFileName(absolutePath);

        // Match against multiple candidate strings so callers can target absolute paths,
        // normalized paths, relative paths, file names, or solution project names.
        var candidates = new[]
        {
            absolutePath,
            absolutePath.Replace('\\', '/'),
            relativePath,
            relativePath.Replace('\\', '/'),
            fileName,
            project.ProjectName
        };

        // Any regex can match any candidate: arrays behave as OR sets.
        return regexes.Any(regex => candidates.Any(candidate => regex.IsMatch(candidate)));
    }

    /// <summary>
    /// Loads project entries from a supported solution format.
    /// </summary>
    /// <param name="solutionFilePath">The full solution file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The project entries available in the solution.</returns>
    /// <exception cref="DependencyGeneratorException">Thrown when the extension is unsupported or the solution cannot be parsed.</exception>
    private async Task<IReadOnlyList<SolutionProjectDescriptor>> GetSolutionProjectsAsync(string solutionFilePath, CancellationToken cancellationToken)
    {
        if (_hasCachedProjects && string.Equals(_cachedSolutionFilePath, solutionFilePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Using cached solution projects for {SolutionPath}", Path.GetFileName(solutionFilePath));

            return _cachedProjects;
        }

        _logger.LogDebug("Loading solution projects from {SolutionPath}", Path.GetFileName(solutionFilePath));

        var extension = Path.GetExtension(solutionFilePath);

        if (!_solutionProjectResolvers.TryGetValue(extension, out var resolver))
        {
            var supportedExtensions = string.Join(", ", _solutionProjectResolvers.Keys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase));
            throw new DependencyGeneratorException($"Unsupported solution extension '{extension}'. Supported extensions are {supportedExtensions}. Path: {solutionFilePath}");
        }

        IReadOnlyList<SolutionProjectDescriptor> solutionProjects;

        try
        {
            solutionProjects = await resolver.GetProjectsAsync(solutionFilePath, cancellationToken).ConfigureAwait(false);
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
