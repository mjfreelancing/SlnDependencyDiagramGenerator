using AllOverIt.Extensions;
using AllOverIt.IO;
using AllOverIt.Patterns.Specification.Extensions;
using AllOverIt.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Renderers;
using SlnDependencyDiagramGenerator.Renderers.D2;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Generates dependency summaries and diagrams for projects in a Visual Studio solution.</summary>
/// <remarks>
/// Matching projects are selected using include/exclude regex rules from <see cref="DependencyGeneratorConfig"/>.
/// <para>
/// Project and framework references are read from evaluated MSBuild items, while package references
/// (explicit and transitive) are resolved from each project's <c>project.assets.json</c> file.
/// This assets file is written by <c>dotnet restore</c> and is the authoritative source for
/// resolved package versions, including Central Package Management and Directory.Build.props effects.
/// </para>
/// <para>
/// Output includes a markdown summary plus D2 and/or Mermaid diagrams for individual projects and/or
/// the full selected solution scope, with optional <c>svg</c>, <c>png</c>, and <c>pdf</c> image export.
/// </para>
/// </remarks>
public sealed class DependencyGenerator : IDependencyGenerator
{
    private readonly record struct PackageVersion(string Name, string Version);

    private readonly IProjectDiscoveryService _projectDiscovery;
    private readonly IToolDetectionService _toolDetection;
    private readonly IToolPathResolver _toolPathResolver;
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DependencyGenerator> _logger;
    /// <summary>Initializes a new dependency generator instance with explicitly provided services (for DI).</summary>
    /// <param name="projectDiscovery">The project discovery service used for parsing solutions and resolving dependencies.</param>
    /// <param name="toolDetection">The tool detection service used for checking external CLI tool availability.</param>
    /// <param name="toolPathResolver">Resolves effective tool paths for external CLI invocation.</param>
    /// <param name="validationInvoker">The validation invoker used to validate configuration before generation.</param>
    /// <param name="loggerFactory">The logger factory used to create loggers for the generator and renderers.</param>
    public DependencyGenerator(IProjectDiscoveryService projectDiscovery, IToolDetectionService toolDetection,
        IToolPathResolver toolPathResolver, IValidationInvoker validationInvoker, ILoggerFactory loggerFactory)
    {
        _projectDiscovery = projectDiscovery;
        _toolDetection = toolDetection;
        _toolPathResolver = toolPathResolver;
        _loggerFactory = loggerFactory;
        _validationInvoker = validationInvoker;
        _logger = loggerFactory.CreateLogger<DependencyGenerator>();
    }

    /// <inheritdoc />
    public void ValidateConfiguration(DependencyGeneratorConfig configuration)
    {
        _validationInvoker.AssertValidation(configuration);
    }

    /// <inheritdoc />
    public async Task CreateDiagramsAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateConfiguration(configuration);

        // Fail early if required external tools are not available.
        await AssertToolAvailabilityAsync(configuration, cancellationToken).ConfigureAwait(false);

        var individualTransitiveDepth = configuration.Solution.Individual.Enabled
            ? configuration.Solution.Individual.TransitiveDepth
            : 0;

        var allTransitiveDepth = configuration.Solution.All.Enabled
            ? configuration.Solution.All.TransitiveDepth
            : 0;

        var maxTransitiveDepth = Math.Max(individualTransitiveDepth, allTransitiveDepth);

        var regexToInclude = configuration.Solution.RegexToInclude;
        var regexToExclude = configuration.Solution.RegexToExclude;
        var excludePackages = configuration.Solution.PackagesToExclude;
        var excludeFrameworks = configuration.Solution.FrameworksToExclude;
        var solutionPath = configuration.Solution.SolutionPath;

        _logger.LogInformation("Starting diagram generation for {SolutionPath}", Path.GetFileName(solutionPath));

        // Target frameworks are auto-discovered from each project's project.assets.json
        var targetFrameworks = await _projectDiscovery
            .DiscoverTargetFrameworksAsync(solutionPath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        if (targetFrameworks.Length == 0)
        {
            _logger.LogDebug("No target frameworks discovered in {SolutionPath}", Path.GetFileName(solutionPath));

            return;
        }

        _logger.LogDebug("Discovered {TargetFrameworkCount} target framework(s): {TargetFrameworks}",
            targetFrameworks.Length, string.Join(", ", targetFrameworks));

        var renderers = GetRenderers(configuration);

        // Log which projects were resolved, included, and excluded.
        await LogProjectDiscoveryAsync(solutionPath, regexToInclude, regexToExclude, cancellationToken).ConfigureAwait(false);

        foreach (var targetFramework in targetFrameworks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parseRequest = new SolutionParseRequest
            {
                SolutionFilePath = solutionPath,
                RegexToInclude = regexToInclude,
                RegexToExclude = regexToExclude,
                ExcludePackages = excludePackages,
                ExcludeFrameworks = excludeFrameworks,
                TargetFramework = targetFramework,
                MaxTransitiveDepth = maxTransitiveDepth
            };

            var allProjects = await _projectDiscovery
                .ParseProjectsAsync(parseRequest, cancellationToken)
                .ConfigureAwait(false);

            if (allProjects.Length == 0)
            {
                var includeRegexList = string.Join(", ", configuration.Solution.RegexToInclude);

                var excludeRegexList = configuration.Solution.RegexToExclude.Length > 0
                    ? string.Join(", ", configuration.Solution.RegexToExclude)
                    : "<none>";

                _logger.LogError(
                    "No projects matched the configured filters for target framework {TargetFramework}",
                    targetFramework);

                continue;
            }

            _logger.LogDebug("Processing target framework: {TargetFramework}", targetFramework);

            foreach (var project in allProjects)
            {
                LogDependencies(project);
            }

            // GroupBy handles duplicate project filenames across different directories
            var solutionProjects = allProjects
                .GroupBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToDictionary(project => project.Name, project => project, StringComparer.OrdinalIgnoreCase);

            var exportPath = Path.Combine(configuration.Export.RootPath, targetFramework);

            Directory.CreateDirectory(exportPath);

            if (configuration.Export.ClearContents)
            {
                ClearFolder(exportPath);
            }

            _logger.LogInformation("Exporting summary for {TargetFramework}…", targetFramework);

            await ExportAsSummaryAsync(exportPath, solutionProjects, cancellationToken).ConfigureAwait(false);

            // Prepare renderer folders up-front so both individual and all-projects output start clean,
            // regardless of which scope(s) are enabled.
            if (configuration.Solution.Individual.Enabled || configuration.Solution.All.Enabled)
            {
                PrepareRendererFolders(exportPath, renderers, configuration.Export.ClearContents);
            }

            if (configuration.Solution.Individual.Enabled)
            {
                _logger.LogInformation("Generating per-project diagrams for {TargetFramework}…", targetFramework);

                await ExportAsIndividualAsync(configuration, targetFramework, exportPath, solutionProjects, renderers, cancellationToken).ConfigureAwait(false);
            }

            if (configuration.Solution.All.Enabled)
            {
                _logger.LogInformation("Generating all-projects diagram for {TargetFramework}…", targetFramework);

                await ExportAsAllAsync(configuration, targetFramework, exportPath, solutionProjects, renderers, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Diagram generation complete");
    }

    private static void ClearFolder(string exportPath)
    {
        var files = FileSearch.GetFiles(exportPath, "*.*", DiskSearchOptions.None);

        foreach (var file in files)
        {
            file.Delete();
        }
    }

    /// <summary>Creates each configured renderer's output folder and, when requested, empties it of existing files.</summary>
    /// <param name="exportPath">The target-framework export path.</param>
    /// <param name="renderers">The configured diagram renderers.</param>
    /// <param name="clearContents">When <see langword="true"/>, existing files in each renderer folder are deleted first.</param>
    private static void PrepareRendererFolders(string exportPath, IDiagramRenderer[] renderers, bool clearContents)
    {
        // Renderer folders are prepared up-front (before both individual and all-projects output) so that
        // either scope starts from a clean folder — otherwise a run that switches scope could leave stale
        // per-project files from a previous run behind.
        foreach (var renderer in renderers)
        {
            var rendererExportPath = Path.Combine(exportPath, renderer.FileExtension);

            Directory.CreateDirectory(rendererExportPath);

            if (clearContents)
            {
                ClearFolder(rendererExportPath);
            }
        }
    }

    private async Task ExportAsIndividualAsync(DependencyGeneratorConfig configuration, string targetFramework,
        string exportPath, IDictionary<string, SolutionProject> solutionProjects, IDiagramRenderer[] renderers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var includeDependencies = configuration.Solution.Individual.IncludeDependencies;
        var transitiveDepth = configuration.Solution.Individual.TransitiveDepth;

        foreach (var renderer in renderers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rendererExportPath = Path.Combine(exportPath, renderer.FileExtension);

            foreach (var scopedProject in solutionProjects.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("  {FileExtension}: {ProjectName}", renderer.FileExtension, scopedProject.Name);

                var packagesWithMultipleVersions = GetDeepOrderedDistinctPackageDependencies(scopedProject, solutionProjects, kvp => kvp.Count() > 1)
                    .ToDictionary(kvp => kvp.Key, kvp => GetDiagramPackageGroupId(kvp.Key));

                var model = BuildGraphModel([scopedProject], solutionProjects, includeDependencies, transitiveDepth, packagesWithMultipleVersions);

                await renderer
                    .CreateDiagramArtifactsAsync(targetFramework, rendererExportPath, scopedProject.Name, model, configuration.Export.ImageFormats, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async Task ExportAsAllAsync(DependencyGeneratorConfig configuration, string targetFramework,
        string exportPath, IDictionary<string, SolutionProject> solutionProjects, IDiagramRenderer[] renderers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var includeDependencies = configuration.Solution.All.IncludeDependencies;
        var transitiveDepth = configuration.Solution.All.TransitiveDepth;

        // Calculated from assets-resolved package graphs across the selected project scope.
        // This flags cross-project version divergence (same package id, different resolved versions).
        var packagesWithMultipleVersions = solutionProjects.Values
            .SelectMany(project => GetAllPackageDependencies(project.PackageReferences))
            .Select(package => new PackageVersion(package.Name, package.Version))
            .Distinct()
            .GroupBy(package => package.Name)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => GetDiagramPackageGroupId(group.Key));

        var model = BuildGraphModel(solutionProjects.Values, solutionProjects, includeDependencies, transitiveDepth, packagesWithMultipleVersions);

        foreach (var renderer in renderers)
        {
            var projectScope = $"{configuration.Diagram.GroupName}-All";
            var rendererExportPath = Path.Combine(exportPath, renderer.FileExtension);

            await renderer
                .CreateDiagramArtifactsAsync(targetFramework, rendererExportPath, projectScope, model, configuration.Export.ImageFormats, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private IDiagramRenderer[] GetRenderers(DependencyGeneratorConfig configuration)
    {
        var diagramOptions = configuration.Diagram;

        return [.. diagramOptions.Formats
            .Distinct()
            .Select<DiagramFormat, IDiagramRenderer>(diagramFormat => diagramFormat switch
            {
                DiagramFormat.Mermaid => new MermaidDiagramRenderer(diagramOptions, _toolPathResolver, _loggerFactory.CreateLogger<MermaidDiagramRenderer>()),
                DiagramFormat.D2 => new D2DiagramRenderer(diagramOptions, _toolPathResolver, _loggerFactory.CreateLogger<D2DiagramRenderer>()),
                _ => throw new ArgumentOutOfRangeException(nameof(diagramFormat))
            })];
    }

    private static DependencyGraphModel BuildGraphModel(IEnumerable<SolutionProject> rootProjects, IDictionary<string, SolutionProject> allProjects,
        bool includeDependencies, int maxTransitiveDepth, IReadOnlyDictionary<string, string> packagesWithMultipleVersions)
    {
        var seen = new HashSet<string>();
        var reachable = new List<SolutionProject>();

        foreach (var root in rootProjects)
        {
            CollectReachableProjects(root, allProjects, seen, reachable);
        }

        var projectNodes = reachable
            .Select(project => BuildProjectNode(project, includeDependencies, maxTransitiveDepth))
            .ToArray();

        return new DependencyGraphModel
        {
            Projects = projectNodes,
            PackagesWithMultipleVersions = packagesWithMultipleVersions
        };
    }

    private static void CollectReachableProjects(SolutionProject project, IDictionary<string, SolutionProject> allProjects,
        HashSet<string> seen, List<SolutionProject> result)
    {
        if (!seen.Add(project.Name))
        {
            return;
        }

        result.Add(project);

        var refPaths = project.ProjectReferences
            .Select(projectReference => projectReference.Path);

        foreach (var refPath in refPaths)
        {
            var refName = Path.GetFileNameWithoutExtension(refPath);

            if (allProjects.TryGetValue(refName, out var referenced))
            {
                CollectReachableProjects(referenced, allProjects, seen, result);
            }
        }
    }

    private static ProjectNode BuildProjectNode(SolutionProject project, bool includeDependencies, int maxTransitiveDepth)
    {
        var projectRefs = project.ProjectReferences
            .Select(projectReference => projectReference.Path)
            .ToArray();

        if (!includeDependencies)
        {
            return new ProjectNode
            {
                Name = project.Name,
                ProjectReferences = projectRefs
            };
        }

        var frameworks = project.FrameworkReferences
            .Select(framework => new FrameworkNode { Name = framework.Name })
            .ToArray();

        var packageList = new List<PackageNode>();
        var dependencies = project.PackageReferences;

        foreach (var dependency in dependencies)
        {
            var node = BuildPackageNode(dependency, maxTransitiveDepth);

            if (node is not null)
            {
                packageList.Add(node);
            }
        }

        return new ProjectNode
        {
            Name = project.Name,
            FrameworkReferences = frameworks,
            PackageReferences = [.. packageList],
            ProjectReferences = projectRefs
        };
    }

    private static PackageNode? BuildPackageNode(PackageReference packageReference, int maxTransitiveDepth)
    {
        if (packageReference.Depth > maxTransitiveDepth)
        {
            return null;
        }

        var children = new List<PackageNode>();

        foreach (var child in packageReference.TransitiveReferences)
        {
            var childNode = BuildPackageNode(child, maxTransitiveDepth);

            if (childNode is not null)
            {
                children.Add(childNode);
            }
        }

        return new PackageNode
        {
            Name = packageReference.Name,
            Version = packageReference.Version,
            IsTransitive = packageReference.IsTransitive,
            Depth = packageReference.Depth,
            TransitiveReferences = [.. children]
        };
    }

    private async Task ExportAsSummaryAsync(string exportPath, IDictionary<string, SolutionProject> solutionProjects, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

        var filename = Path.Combine(exportPath, SummaryDependencyGenerator.MarkdownFilename);

        _logger.LogDebug("Exporting Summary: \"{Filename}\"", filename);

        await File
            .WriteAllTextAsync(filename, content, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Export complete");
    }

    private static string GetProjectName(ProjectReference projectReference)
    {
        return Path.GetFileNameWithoutExtension(projectReference.Path);
    }

    private static string GetDiagramPackageGroupId(string packageName)
    {
        return packageName.Replace(".", "-").ToLowerInvariant();
    }

    private void LogDependencies(SolutionProject solutionProject)
    {
        LogProjectDependencies(solutionProject);
        LogFrameworkDependencies(solutionProject);
        LogPackageDependencies(solutionProject);
    }

    private void LogProjectDependencies(SolutionProject solutionProject)
    {
        var sortedProjectDependenies = solutionProject.ProjectReferences
            .Select(item => item.Path)
            .Order();

        foreach (var dependency in sortedProjectDependenies)
        {
            _logger.LogInformation("{ProjectName} depends on {DependencyName}",
                solutionProject.Name, Path.GetFileNameWithoutExtension(dependency));
        }
    }

    private void LogFrameworkDependencies(SolutionProject solutionProject)
    {
        var sortedFrameworkReferences = solutionProject.FrameworkReferences
            .Select(item => item.Name)
            .Order();

        foreach (var dependency in sortedFrameworkReferences)
        {
            _logger.LogInformation("{ProjectName} depends on {DependencyName}",
                solutionProject.Name, Path.GetFileNameWithoutExtension(dependency));
        }
    }

    private static IEnumerable<IGrouping<string, PackageVersion>> GetOrderedDistinctPackageDependencies(SolutionProject solutionProject,
        Func<IGrouping<string, PackageVersion>, bool>? predicate = null)
    {
        var results = GetAllPackageDependencies(solutionProject.PackageReferences)
            .Select(item => new PackageVersion(item.Name, item.Version))
            .Distinct()                                     // Multiple packages may depend on another common package
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .GroupBy(item => item.Name);

        return predicate is null
            ? results
            : results.Where(predicate);
    }

    // For a given project get an ordered, distinct, list of all package references, including the package references for all referenced projects.
    private static IEnumerable<IGrouping<string, PackageVersion>> GetDeepOrderedDistinctPackageDependencies(SolutionProject solutionProject,
        IDictionary<string, SolutionProject> solutionProjects, Func<IGrouping<string, PackageVersion>, bool>? predicate = null)
    {
        var allPackageDependencies = new List<PackageVersion>();

        GetDeepProjectPackageDependenciesRecursively(solutionProject, solutionProjects, allPackageDependencies);

        var results = allPackageDependencies
            .Distinct()                                     // Multiple packages may depend on another common package
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .GroupBy(item => item.Name);

        return predicate is null
            ? results
            : results.Where(predicate);
    }

    // For a given project find all package references, including the package references for all referenced projects.
    private static void GetDeepProjectPackageDependenciesRecursively(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects,
        List<PackageVersion> allPackageDependencies)
    {
        var packageDependencies = GetAllPackageDependencies(solutionProject.PackageReferences)
            .Select(item => new PackageVersion(item.Name, item.Version))
            .Distinct();

        allPackageDependencies.AddRange(packageDependencies);

        var projectReferences = solutionProject.ProjectReferences
            .Select(GetProjectName)
            .Select(projectName => solutionProjects[projectName]);

        foreach (var projectReference in projectReferences)
        {
            GetDeepProjectPackageDependenciesRecursively(projectReference, solutionProjects, allPackageDependencies);
        }
    }

    private void LogPackageDependencies(SolutionProject solutionProject)
    {
        var sortedPackageDependencies = GetOrderedDistinctPackageDependencies(solutionProject);

        foreach (var dependency in sortedPackageDependencies)
        {
            var dependencyName = dependency.Key;
            var dependencyVersions = dependency.ToList();

            if (dependencyVersions.Count == 1)
            {
                var dependencyVersion = dependencyVersions.Single();

                _logger.LogInformation("{ProjectName} depends on {PackageName} v{Version}",
                    solutionProject.Name, dependencyName, dependencyVersion.Version);
            }
            else
            {
                var versions = dependencyVersions.Select(item => $"v{item.Version}");

                _logger.LogError("{ProjectName} depends on multiple versions of {PackageName}: {Versions}",
                    solutionProject.Name, dependencyName, string.Join(", ", versions));
            }
        }
    }

    private static IEnumerable<PackageReference> GetAllPackageDependencies(IEnumerable<PackageReference> packageReferences)
    {
        foreach (var packageReference in packageReferences)
        {
            yield return packageReference;

            foreach (var transitiveReference in GetAllPackageDependencies(packageReference.TransitiveReferences))
            {
                yield return transitiveReference;
            }
        }
    }

    private async Task AssertToolAvailabilityAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken)
    {
        // External tools are only required for image export. Generating the diagram
        // text files is fully in-process, so text-only generation must work without d2/mmdc.
        if (configuration.Export.ImageFormats.Length == 0)
        {
            return;
        }

        var formats = configuration.Diagram.Formats;

        if (formats.Length == 0)
        {
            return;
        }

        var tasks = formats
            .Distinct()
            .Select(async format =>
            {
                return await _toolDetection.CheckToolAvailabilityAsync(
                    _toolPathResolver.GetToolName(format), cancellationToken: cancellationToken).ConfigureAwait(false);
            });

        var statuses = await Task.WhenAll(tasks).ConfigureAwait(false);

        if (statuses.All(status => status.IsAvailable))
        {
            return;
        }

        var unavailable = statuses
            .Where(status => !status.IsAvailable)
            .Select(status => status.ToolName);

        var message = $"Required external tools are not available: {string.Join(", ", unavailable)}";

        throw new ToolNotFoundException(message);
    }

    private async Task LogProjectDiscoveryAsync(string solutionPath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        var result = await _projectDiscovery
            .DiscoverProjectsAsync(solutionPath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Projects in solution: {ProjectCount}", result.AllProjectPaths.Length);

        LogIncludedProjects(result.IncludedProjectPaths);

        LogExcludedProjects(result.ExcludedProjectPaths);

        LogImplicitlyExcludedProjects(result.ImplicitlyExcludedProjectPaths);
    }

    private void LogIncludedProjects(string[] includedPaths)
    {
        var ordered = includedPaths
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation("  Included (will be processed): {Count} project(s)", ordered.Length);

        if (ordered.Length == 0)
        {
            return;
        }

        foreach (var path in ordered)
        {
            _logger.LogInformation("    - {ProjectName}", Path.GetFileName(path));
        }
    }

    private void LogExcludedProjects(string[] excludedPaths)
    {
        var ordered = excludedPaths
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation("  Excluded (matched exclude regex): {Count} project(s)", ordered.Length);

        if (ordered.Length == 0)
        {
            return;
        }

        foreach (var path in ordered)
        {
            _logger.LogInformation("    - {ProjectName}", Path.GetFileName(path));
        }
    }

    private void LogImplicitlyExcludedProjects(string[] implicitlyExcludedPaths)
    {
        var ordered = implicitlyExcludedPaths
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation("  Implicitly excluded (did not match include regex): {Count} project(s)", ordered.Length);

        if (ordered.Length == 0)
        {
            return;
        }

        foreach (var path in ordered)
        {
            _logger.LogInformation("    - {ProjectName}", Path.GetFileName(path));
        }
    }
}
