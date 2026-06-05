using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.IO;
using AllOverIt.Logging;
using AllOverIt.Patterns.Specification.Extensions;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Renderers;
using SlnDependencyDiagramGenerator.Renderers.D2;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;
using SlnDependencyDiagramGenerator.Validators;
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
public sealed class DependencyGenerator
{
    private readonly record struct PackageVersion(string Name, string Version);

    private readonly IProjectDiscoveryService _projectDiscovery;
    private readonly IToolDetectionService _toolDetection;
    private readonly IColorConsoleLogger _logger;

    /// <summary>Initializes a new dependency generator with default service implementations.
    /// Intended for simple usage where DI is not required.</summary>
    public DependencyGenerator()
        : this(new ProjectDiscoveryService(), new ToolDetectionService(), new ColorConsoleLogger())
    {
    }

    /// <summary>Initializes a new dependency generator instance with explicitly provided services (for DI).</summary>
    /// <param name="projectDiscovery">The project discovery service used for parsing solutions and resolving dependencies.</param>
    /// <param name="toolDetection">The tool detection service used for checking external CLI tool availability.</param>
    /// <param name="logger">A console logger that provides progress information during the processing of projects and generation of diagrams.</param>
    public DependencyGenerator(IProjectDiscoveryService projectDiscovery, IToolDetectionService toolDetection, IColorConsoleLogger logger)
    {
        _projectDiscovery = projectDiscovery.WhenNotNull();
        _toolDetection = toolDetection.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <summary>Generates dependency summaries, diagram files, and optional images for each discovered target framework.</summary>
    /// <param name="configuration">The dependency generator configuration options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the diagram generation has completed.</returns>
    public async Task CreateDiagramsAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AssertConfiguration(configuration);

        var individualTransitiveDepth = configuration.Projects.Individual.Enabled
            ? configuration.Projects.Individual.TransitiveDepth
            : 0;

        var allTransitiveDepth = configuration.Projects.All.Enabled
            ? configuration.Projects.All.TransitiveDepth
            : 0;

        var maxTransitiveDepth = Math.Max(individualTransitiveDepth, allTransitiveDepth);

        var regexToInclude = configuration.Projects.RegexToInclude;
        var regexToExclude = configuration.Projects.RegexToExclude;
        var excludePackages = configuration.Projects.PackagesToExclude;
        var excludeFrameworks = configuration.Projects.FrameworksToExclude;
        var solutionPath = configuration.Projects.SolutionPath;

        // Target frameworks are auto-discovered from each project's project.assets.json
        var targetFrameworks = await _projectDiscovery
            .DiscoverTargetFrameworksAsync(solutionPath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        if (targetFrameworks.Length == 0)
        {
            _logger
                .Write(ConsoleColor.Red, "No target frameworks discovered in ")
                .WriteLine(ConsoleColor.Yellow, Path.GetFileName(solutionPath));

            return;
        }

        var renderers = GetRenderers(configuration);

        // Check tool availability and log warnings before processing projects,
        // so failures are visible early even if generation continues.
        await LogToolAvailabilityAsync(configuration, cancellationToken).ConfigureAwait(false);

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
                var includeRegexList = string.Join(", ", configuration.Projects.RegexToInclude);

                var excludeRegexList = configuration.Projects.RegexToExclude.Length > 0
                    ? string.Join(", ", configuration.Projects.RegexToExclude)
                    : "<none>";

                _logger
                    .WriteLine(ConsoleColor.Red, "No projects found with the configured filters:")
                    .Write(ConsoleColor.DarkGray, "  Solution path: ")
                    .WriteLine(ConsoleColor.Yellow, solutionPath)
                    .Write(ConsoleColor.DarkGray, "  Include regex(es): ")
                    .WriteLine(ConsoleColor.Yellow, includeRegexList)
                    .Write(ConsoleColor.DarkGray, "  Exclude regex(es): ")
                    .WriteLine(ConsoleColor.Yellow, excludeRegexList)
                    .Write(ConsoleColor.DarkGray, "  Target framework: ")
                    .WriteLine(ConsoleColor.Yellow, targetFramework)
                    .WriteLine();

                continue;
            }

            _logger
                .Write(ConsoleColor.White, "Processing target framework: ")
                .WriteLine(ConsoleColor.Yellow, targetFramework)
                .WriteLine();

            foreach (var project in allProjects)
            {
                LogDependencies(project);
            }

            _logger.WriteLine();

            var solutionProjects = allProjects.ToDictionary(project => project.Name, project => project);

            var exportPath = Path.Combine(configuration.Export.RootPath, targetFramework);

            Directory.CreateDirectory(exportPath);

            if (configuration.Export.ClearContents)
            {
                ClearFolder(exportPath);
            }

            await ExportAsSummaryAsync(exportPath, solutionProjects, cancellationToken).ConfigureAwait(false);

            if (configuration.Projects.Individual.Enabled)
            {
                await ExportAsIndividualAsync(configuration, targetFramework, exportPath, solutionProjects, renderers, cancellationToken).ConfigureAwait(false);
            }

            if (configuration.Projects.All.Enabled)
            {
                await ExportAsAllAsync(configuration, targetFramework, exportPath, solutionProjects, renderers, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void ClearFolder(string exportPath)
    {
        var files = FileSearch.GetFiles(exportPath, "*.*", DiskSearchOptions.None);

        foreach (var file in files)
        {
            file.Delete();
        }
    }

    private async Task ExportAsIndividualAsync(DependencyGeneratorConfig configuration, string targetFramework, string exportPath, IDictionary<string, SolutionProject> solutionProjects,
        IDiagramRenderer[] renderers, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var includeDependencies = configuration.Projects.Individual.IncludeDependencies;
        var transitiveDepth = configuration.Projects.Individual.TransitiveDepth;

        foreach (var scopedProject in solutionProjects.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var packagesWithMultipleVersions = GetDeepOrderedDistinctPackageDependencies(scopedProject, solutionProjects, kvp => kvp.Count() > 1)
                .ToDictionary(kvp => kvp.Key, kvp => GetDiagramPackageGroupId(kvp.Key));

            var model = BuildGraphModel([scopedProject], solutionProjects, includeDependencies, transitiveDepth, packagesWithMultipleVersions);

            foreach (var renderer in renderers)
            {
                await renderer
                    .CreateDiagramArtifactsAsync(targetFramework, exportPath, scopedProject.Name, model, configuration.Export.ImageFormats, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.WriteLine();
        }
    }

    private async Task ExportAsAllAsync(DependencyGeneratorConfig configuration, string targetFramework, string exportPath, IDictionary<string, SolutionProject> solutionProjects,
        IDiagramRenderer[] renderers, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var includeDependencies = configuration.Projects.All.IncludeDependencies;
        var transitiveDepth = configuration.Projects.All.TransitiveDepth;

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

            await renderer
                .CreateDiagramArtifactsAsync(targetFramework, exportPath, projectScope, model, configuration.Export.ImageFormats, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.WriteLine();
    }

    private IDiagramRenderer[] GetRenderers(DependencyGeneratorConfig configuration)
    {
        var diagramOptions = configuration.Diagram;

        return [.. diagramOptions.Formats
            .Distinct()
            .Select<DiagramFormat, IDiagramRenderer>(diagramFormat => diagramFormat switch
            {
                DiagramFormat.Mermaid => new MermaidDiagramRenderer(diagramOptions, _logger),
                DiagramFormat.D2 => new D2DiagramRenderer(diagramOptions, _logger),
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

    private static PackageNode BuildPackageNode(PackageReference packageReference, int maxTransitiveDepth)
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

        _logger
            .Write("{forecolor:white}Exporting Summary: ")
            .Write(ConsoleColor.Yellow, filename)
            .Write("{forecolor:white}...");

        await File
            .WriteAllTextAsync(filename, content, cancellationToken)
            .ConfigureAwait(false);

        _logger.WriteLine("{forecolor:green}Done");
        _logger.WriteLine();
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
            _logger
                .Write(ConsoleColor.Yellow, solutionProject.Name)
                .Write(ConsoleColor.White, " depends on ")
                .WriteLine(ConsoleColor.Yellow, Path.GetFileNameWithoutExtension(dependency));
        }
    }

    private void LogFrameworkDependencies(SolutionProject solutionProject)
    {
        var sortedFrameworkReferences = solutionProject.FrameworkReferences
            .Select(item => item.Name)
            .Order();

        foreach (var dependency in sortedFrameworkReferences)
        {
            _logger
                .Write(ConsoleColor.Yellow, solutionProject.Name)
                .Write(ConsoleColor.White, " depends on ")
                .WriteLine(ConsoleColor.Yellow, Path.GetFileNameWithoutExtension(dependency));
        }
    }

    private static IEnumerable<IGrouping<string, PackageVersion>> GetOrderedDistinctPackageDependencies(SolutionProject solutionProject,
        Func<IGrouping<string, PackageVersion>, bool> predicate = default)
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
        IDictionary<string, SolutionProject> solutionProjects, Func<IGrouping<string, PackageVersion>, bool> predicate = default)
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

                _logger
                    .Write(ConsoleColor.Yellow, solutionProject.Name)
                    .Write(ConsoleColor.White, " depends on ")
                    .WriteLine(ConsoleColor.Yellow, $"{dependencyName} v{dependencyVersion.Version}");
            }
            else
            {
                var versions = dependencyVersions.Select(item => $"v{item.Version}");

                _logger
                    .WriteLine(ConsoleColor.Red, $"{solutionProject.Name} depends on multiple versions of {dependencyName} {string.Join(", ", versions)}");
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

    private async Task LogToolAvailabilityAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken)
    {
        if (configuration.Export.ImageFormats.Length == 0)
        {
            return;
        }

        var readiness = await _toolDetection
            .CheckConfiguredToolsAsync(configuration.Export.ImageFormats, cancellationToken)
            .ConfigureAwait(false);

        if (!readiness.AllRequiredToolsAvailable)
        {
            foreach (var status in readiness.ToolStatuses)
            {
                if (!status.IsAvailable)
                {
                    _logger.WriteLine(ConsoleColor.Red, status.ErrorMessage ?? $"'{status.ToolName}' is not available.");
                }
            }
        }
    }

    private static void AssertConfiguration(DependencyGeneratorConfig configuration)
    {
        var validator = new DependencyGeneratorConfigValidator();
        validator.ValidateAndThrow(configuration);
    }
}