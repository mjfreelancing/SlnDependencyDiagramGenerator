using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.IO;
using AllOverIt.Logging;
using AllOverIt.Patterns.Specification.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Validators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Parses a Visual Studio Solution file to discover the projects it contains. These projects are then filtered based on
/// one or more regex expressions, allowing for projects to be filtered based on their name or folder location. Each project is
/// then parsed to discover any dependent <see cref="ProjectReference"/>, explicit and transitive (implicit) <see cref="PackageReference"/>,
/// and <see cref="FrameworkReference"/> elements.<br/><br/>
/// Package references are resolved from each project's <c>project.assets.json</c> file (written by <c>dotnet restore</c>),
/// which is the authoritative source for fully-resolved package versions, Central Package Management, and Directory.Build.props.
/// <c>dotnet restore</c> must be run before calling <see cref="CreateDiagramsAsync"/>.<br/><br/>
/// With all of this information the dependency generator creates a 'Dependency Summary' markdown report, a dependency diagram for each
/// project as well as the entire solution (for the projects processed) in D2 and/or Mermaid format, with optional export to one or more
/// of the <c>svg</c>, <c>png</c>, or <c>pdf</c> image formats.<br/><br/>
/// Refer to <see cref="DependencyGeneratorConfig"/> for more information on the configuration options available.</summary>
public sealed partial class DependencyGenerator
{
    private readonly DependencyGeneratorConfig _configuration;
    private readonly IColorConsoleLogger _logger;

    /// <summary>Initializes a new dependency generator instance.</summary>
    /// <param name="configuration">The dependency generator configuration options.</param>
    /// <param name="logger">A console logger that provides progress information during the processing of projects and generation of diagrams.</param>
    public DependencyGenerator(DependencyGeneratorConfig configuration, IColorConsoleLogger logger)
    {
        _configuration = configuration.WhenNotNull();
        _logger = logger.WhenNotNull();

        AssertConfiguration();
    }

    /// <summary>Generates dependency summaries, diagram files, and optional images for each discovered target framework.</summary>
    /// <returns>A <see cref="Task"/> that completes when the diagram generation has completed.</returns>
    public async Task CreateDiagramsAsync()
    {
        var individualTransitiveDepth = _configuration.Projects.Individual.Enabled ? _configuration.Projects.Individual.TransitiveDepth : 0;
        var allTransitiveDepth = _configuration.Projects.All.Enabled ? _configuration.Projects.All.TransitiveDepth : 0;
        var maxTransitiveDepth = Math.Max(individualTransitiveDepth, allTransitiveDepth);

        var regexToInclude = _configuration.Projects.RegexToInclude;
        var regexToExclude = _configuration.Projects.RegexToExclude;
        var excludePackages = _configuration.Projects.PackagesToExclude;
        var solutionPath = _configuration.Projects.SolutionPath;

        var solutionParser = new SolutionParser();

        // Target frameworks are auto-discovered from each project's project.assets.json;
        // the 'targetFrameworks' config field has been removed in v4.
        var targetFrameworks = solutionParser.DiscoverTargetFrameworks(solutionPath, regexToInclude, regexToExclude);

        if (targetFrameworks.Length == 0)
        {
            _logger
                .Write(ConsoleColor.Red, "No target frameworks discovered in ")
                .WriteLine(ConsoleColor.Yellow, Path.GetFileName(solutionPath));

            return;
        }

        await ValidateRequiredToolsAsync().ConfigureAwait(false);

        foreach (var targetFramework in targetFrameworks)
        {
            var allProjects = solutionParser.Parse(solutionPath, regexToInclude, regexToExclude, excludePackages, targetFramework, maxTransitiveDepth);

            if (allProjects.Length == 0)
            {
                _logger
                    .Write(ConsoleColor.Red, "No projects found in ")
                    .Write(ConsoleColor.Yellow, Path.GetFileName(_configuration.Projects.SolutionPath))
                    .Write(ConsoleColor.Red, " using the regex(es) ")
                    .Write(ConsoleColor.Yellow, string.Join(", ", _configuration.Projects.RegexToInclude))
                    .Write(ConsoleColor.Red, " and target framework ")
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

            var exportPath = Path.Combine(_configuration.Export.RootPath, targetFramework);

            Directory.CreateDirectory(exportPath);

            if (_configuration.Export.ClearContents)
            {
                ClearFolder(exportPath);
            }

            await ExportAsSummary(exportPath, solutionProjects).ConfigureAwait(false);

            if (_configuration.Projects.Individual.Enabled)
            {
                await ExportAsIndividual(targetFramework, exportPath, solutionProjects).ConfigureAwait(false);
            }

            if (_configuration.Projects.All.Enabled)
            {
                await ExportAsAll(targetFramework, exportPath, solutionProjects).ConfigureAwait(false);
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

    private async Task ExportAsIndividual(string targetFramework, string exportPath, IDictionary<string, SolutionProject> solutionProjects)
    {
        var renderers = GetRenderers();
        var includeDeps = _configuration.Projects.Individual.IncludeDependencies;
        var transitiveDepth = _configuration.Projects.Individual.TransitiveDepth;

        foreach (var scopedProject in solutionProjects.Values)
        {
            var packagesWithMultipleVersions = GetDeepOrderedDistinctPackageDependencies(scopedProject, solutionProjects, kvp => kvp.Count() > 1)
                .ToDictionary(kvp => kvp.Key, kvp => GetDiagramPackageGroupId(kvp.Key));

            var model = BuildGraphModel([scopedProject], solutionProjects, includeDeps, transitiveDepth, packagesWithMultipleVersions);

            foreach (var renderer in renderers)
            {
                await CreateDiagramFileAndImages(targetFramework, exportPath, scopedProject.Name, renderer, model).ConfigureAwait(false);
            }

            _logger.WriteLine();
        }
    }

    private async Task ExportAsAll(string targetFramework, string exportPath, IDictionary<string, SolutionProject> solutionProjects)
    {
        var renderers = GetRenderers();
        var includeDeps = _configuration.Projects.All.IncludeDependencies;
        var transitiveDepth = _configuration.Projects.All.TransitiveDepth;

        // Calculated from assets-resolved package graphs across the selected project scope.
        // This flags cross-project version divergence (same package id, different resolved versions),
        // not unresolved NuGet restore conflicts within a single project.
        var packagesWithMultipleVersions = solutionProjects.Values
            .SelectMany(project => project.Dependencies.SelectMany(dependency => GetAllPackageDependencies(dependency.PackageReferences)))
            .Select(package => (package.Name, package.Version))
            .Distinct()
            .GroupBy(package => package.Name)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => GetDiagramPackageGroupId(group.Key));

        var model = BuildGraphModel(solutionProjects.Values, solutionProjects, includeDeps, transitiveDepth, packagesWithMultipleVersions);

        foreach (var renderer in renderers)
        {
            await CreateDiagramFileAndImages(targetFramework, exportPath, $"{_configuration.Diagram.GroupName}-All", renderer, model).ConfigureAwait(false);
        }

        _logger.WriteLine();
    }

    private IDiagramRenderer[] GetRenderers()
    {
        var diagramOptions = _configuration.Diagram;

        return diagramOptions.Formats
            .Distinct()
            .Select<DiagramFormat, IDiagramRenderer>(diagramFormat => diagramFormat switch
            {
                DiagramFormat.Mermaid => new MermaidDiagramRenderer(diagramOptions),
                _ => new D2DiagramRenderer(diagramOptions)
            })
            .ToArray();
    }

    private static DependencyGraphModel BuildGraphModel(
        IEnumerable<SolutionProject> rootProjects,
        IDictionary<string, SolutionProject> allProjects,
        bool includeDependencies,
        int maxTransitiveDepth,
        IReadOnlyDictionary<string, string> packagesWithMultipleVersions)
    {
        var seen = new HashSet<string>();
        var reachable = new List<SolutionProject>();

        foreach (var root in rootProjects)
        {
            CollectReachableProjects(root, allProjects, seen, reachable);
        }

        var projectNodes = reachable
            .Select(p => BuildProjectNode(p, includeDependencies, maxTransitiveDepth))
            .ToList();

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

        foreach (var refPath in project.Dependencies.SelectMany(dependency => dependency.ProjectReferences).Select(projectReference => projectReference.Path))
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
        var projectRefs = project.Dependencies
            .SelectMany(dependency => dependency.ProjectReferences)
            .Select(projectReference => projectReference.Path)
            .ToList();

        if (!includeDependencies)
        {
            return new ProjectNode { Name = project.Name, ProjectReferences = projectRefs };
        }

        var frameworks = project.Dependencies
            .SelectMany(dependency => dependency.FrameworkReferences)
            .Select(framework => new FrameworkNode { Name = framework.Name })
            .ToList();

        var packageList = new List<PackageNode>();

        foreach (var dependency in project.Dependencies.SelectMany(projectDependency => projectDependency.PackageReferences))
        {
            var node = BuildPackageNode(dependency, maxTransitiveDepth);

            if (node != null)
            {
                packageList.Add(node);
            }
        }

        return new ProjectNode
        {
            Name = project.Name,
            FrameworkReferences = frameworks,
            PackageReferences = packageList,
            ProjectReferences = projectRefs
        };
    }

    private static PackageNode BuildPackageNode(PackageReference pkg, int maxTransitiveDepth)
    {
        if (pkg.Depth > maxTransitiveDepth)
        {
            return null;
        }

        var children = new List<PackageNode>();

        foreach (var child in pkg.TransitiveReferences)
        {
            var childNode = BuildPackageNode(child, maxTransitiveDepth);

            if (childNode != null)
            {
                children.Add(childNode);
            }
        }

        return new PackageNode
        {
            Name = pkg.Name,
            Version = pkg.Version,
            IsTransitive = pkg.IsTransitive,
            Depth = pkg.Depth,
            TransitiveReferences = children
        };
    }

    private async Task CreateDiagramFileAndImages(string targetFramework, string exportPath, string projectScope,
        IDiagramRenderer renderer, DependencyGraphModel model)
    {
        var content = renderer.Render(model);
        var baseName = GetDiagramAliasId(projectScope, false);

        var rendererExportPath = Path.Combine(exportPath, renderer.FileExtension);
        Directory.CreateDirectory(rendererExportPath);

        var fileName = Path.Combine(rendererExportPath, $"{baseName}.{renderer.FileExtension}");

        if (renderer.FileExtension == "d2")
        {
            await CreateD2FileAsync(targetFramework, fileName, content).ConfigureAwait(false);

            foreach (var format in _configuration.Export.ImageFormats)
            {
                await ExportD2ImageFileAsync(fileName, format).ConfigureAwait(false);
            }
        }
        else
        {
            await CreateMmdFileAsync(targetFramework, fileName, content).ConfigureAwait(false);

            foreach (var format in _configuration.Export.ImageFormats)
            {
                await ExportMmdImageFileAsync(fileName, format).ConfigureAwait(false);
            }
        }
    }

    private async Task CreateMmdFileAsync(string targetFramework, string fileName, string content)
    {
        _logger.Write($"{{forecolor:white}}Creating {{forecolor:yellow}}'{targetFramework}'{{forecolor:white}} diagram: ")
               .Write(ConsoleColor.Yellow, Path.GetFileName(fileName))
               .Write("{forecolor:white}...");

        await File.WriteAllTextAsync(fileName, content).ConfigureAwait(false);

        _logger.WriteLine("{forecolor:green}Done");
    }

    private async Task ExportMmdImageFileAsync(string mmdFileName, DiagramImageFormat format)
    {
        var imageFileName = Path.ChangeExtension(mmdFileName, format.ToString().ToLowerInvariant());

        _logger
            .Write(ConsoleColor.White, "Creating image: ")
            .Write(ConsoleColor.Yellow, Path.GetFileName(imageFileName))
            .Write(ConsoleColor.White, "...");

        var stopwatch = Stopwatch.StartNew();

        // On Windows, npm installs mmdc as mmdc.cmd (not mmdc.exe). CreateProcess does not perform
        // PATHEXT expansion, so we must go through cmd.exe /c to let the shell resolve the .cmd extension.
        var (mmdcExe, mmdcArgs) = OperatingSystem.IsWindows()
            ? ("cmd.exe", new[] { "/c", "mmdc", "-i", mmdFileName, "-o", imageFileName, "--scale", "4" })
            : ("mmdc", ["-i", mmdFileName, "-o", imageFileName, "--scale", "4"]);

        var mmdProcess = ProcessBuilder
            .For(mmdcExe)
            .WithNoWindow()
            .WithArguments(mmdcArgs)
            .WithErrorOutputHandler((sender, eventArgs) =>
            {
                if (eventArgs.Data is string message)
                {
                    _logger.WriteLine(ConsoleColor.Red, $"  {message}");
                }
            })
            .BuildProcessExecutor();

        _ = await mmdProcess.ExecuteAsync();

        stopwatch.Stop();

        _logger.WriteLine(ConsoleColor.Green, $"Done ({FormatElapsed(stopwatch.Elapsed)})");
    }

    private async Task ExportAsSummary(string exportPath, IDictionary<string, SolutionProject> solutionProjects)
    {
        var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

        var filename = Path.Combine(exportPath, SummaryDependencyGenerator.MarkdownFilename);

        _logger
            .Write("{forecolor:white}Exporting Summary: ")
            .Write(ConsoleColor.Yellow, filename)
            .Write("{forecolor:white}...");

        await File.WriteAllTextAsync(filename, content);

        _logger.WriteLine("{forecolor:green}Done");
        _logger.WriteLine();
    }

    private static string GetProjectName(ProjectReference projectReference)
    {
        return Path.GetFileNameWithoutExtension(projectReference.Path);
    }

    private string GetDiagramAliasId(string alias, bool includeProjectGroupPrefix)
    {
        alias = alias.Replace(".", "-").ToLowerInvariant();

        return includeProjectGroupPrefix
            ? $"{_configuration.Diagram.GroupNameAlias}.{alias}"
            : alias;
    }

    private static string GetDiagramPackageGroupId(string packageName)
    {
        return packageName.Replace(".", "-").ToLowerInvariant();
    }

    private async Task CreateD2FileAsync(string targetFramework, string fileName, string content)
    {
        // Showing how to mix AddFormatted() with AddFragment() where the latter
        // is a simple alternative to using string interpolation.
        _logger.Write($"{{forecolor:white}}Creating {{forecolor:yellow}}'{targetFramework}'{{forecolor:white}} diagram: ")
               .Write(ConsoleColor.Yellow, Path.GetFileName(fileName))
               .Write("{forecolor:white}...");

        File.WriteAllText(fileName, content);

        await ProcessBuilder
            .For("d2.exe")
            .WithArguments("fmt", fileName)
            .BuildProcessExecutor()
            .ExecuteAsync();

        // An example using formatted text
        _logger.WriteLine("{forecolor:green}Done");
    }

    private async Task ExportD2ImageFileAsync(string d2FileName, DiagramImageFormat format)
    {
        var imageFileName = Path.ChangeExtension(d2FileName, format.ToString().ToLowerInvariant());

        _logger
            .Write(ConsoleColor.White, "Creating image: ")
            .Write(ConsoleColor.Yellow, Path.GetFileName(imageFileName))
            .Write(ConsoleColor.White, "...");

        var stopwatch = Stopwatch.StartNew();

        // D2 sends all output to stderr — "err:" lines are errors, everything else is info/success.
        var d2Process = ProcessBuilder
            .For("d2.exe")
            .WithNoWindow()
            .WithArguments("-l", "elk", d2FileName, imageFileName)
            .WithErrorOutputHandler((sender, eventArgs) =>
            {
                if (eventArgs.Data is string message)
                {
                    if (message.StartsWith("err:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.WriteLine(ConsoleColor.Red, $"  {message}");
                    }
                    else
                    {
                        // Non-error stderr from d2 is informational.
                    }
                }
            })
            .BuildProcessExecutor();

        _ = await d2Process.ExecuteAsync();

        stopwatch.Stop();

        _logger.WriteLine(ConsoleColor.Green, $"Done ({FormatElapsed(stopwatch.Elapsed)})");
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.TotalSeconds >= 1d
            ? $"{elapsed.TotalSeconds:0.0000000}s"
            : $"{elapsed.TotalMilliseconds:0.0000}ms";
    }

    private void LogDependencies(SolutionProject solutionProject)
    {
        LogProjectDependencies(solutionProject);
        LogFrameworkDependencies(solutionProject);
        LogPackageDependencies(solutionProject);
    }

    private void LogProjectDependencies(SolutionProject solutionProject)
    {
        var sortedProjectDependenies = solutionProject.Dependencies
            .SelectMany(item => item.ProjectReferences)
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
        var sortedFrameworkReferences = solutionProject.Dependencies
            .SelectMany(item => item.FrameworkReferences)
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

    private static IEnumerable<IGrouping<string, (string Name, string Version)>> GetOrderedDistinctPackageDependencies(SolutionProject solutionProject,
        Func<IGrouping<string, (string Name, string Version)>, bool> predicate = default)
    {
        var results = solutionProject.Dependencies
            .SelectMany(item => GetAllPackageDependencies(item.PackageReferences))
            .Select(item => (item.Name, item.Version))
            .Distinct()                                     // Multiple packages may depend on another common package
            .Order()
            .GroupBy(item => item.Name);

        return predicate is null
            ? results
            : results.Where(predicate);
    }

    // For a given project get an ordered, distinct, list of all package references, including the package references for all referenced projects.
    private static IEnumerable<IGrouping<string, (string Name, string Version)>> GetDeepOrderedDistinctPackageDependencies(SolutionProject solutionProject,
        IDictionary<string, SolutionProject> solutionProjects, Func<IGrouping<string, (string Name, string Version)>, bool> predicate = default)
    {
        var allPackageDependencies = new List<(string Name, string Version)>();

        GetDeepProjectPackageDependenciesRecursively(solutionProject, solutionProjects, allPackageDependencies);

        var results = allPackageDependencies
            .Distinct()                                     // Multiple packages may depend on another common package
            .Order()
            .GroupBy(item => item.Name);

        return predicate is null
            ? results
            : results.Where(predicate);
    }

    // For a given project find all package references, including the package references for all referenced projects.
    private static void GetDeepProjectPackageDependenciesRecursively(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects,
        List<(string Name, string Version)> allPackageDependencies)
    {
        var packageDependencies = solutionProject.Dependencies
            .SelectMany(item => GetAllPackageDependencies(item.PackageReferences))
            .Select(item => (item.Name, item.Version))
            .Distinct();

        allPackageDependencies.AddRange(packageDependencies);

        var projectReferences = solutionProject.Dependencies
            .SelectMany(item => item.ProjectReferences)
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

    private async Task ValidateRequiredToolsAsync()
    {
        var formats = _configuration.Diagram.Formats;

        if (formats.Contains(DiagramFormat.D2))
        {
            if (!await IsToolAvailableAsync("d2").ConfigureAwait(false))
            {
                throw new DependencyGeneratorException(
                    "'d2' was not found on PATH. Install it from: https://d2lang.com/tour/install");
            }
        }

        var needsMmdc = formats.Contains(DiagramFormat.Mermaid)
            && _configuration.Export.ImageFormats.Length > 0;

        if (needsMmdc && !await IsToolAvailableAsync("mmdc").ConfigureAwait(false))
        {
            throw new DependencyGeneratorException(
                "'mmdc' was not found on PATH. Install it with: npm install -g @mermaid-js/mermaid-cli");
        }
    }

    // Uses 'where' (Windows) / 'which' (Unix) — the same PATH resolution the shell uses, including
    // PATHEXT expansion. ExecuteBufferedAsync captures stdout/stderr silently with no console noise.
    private static async Task<bool> IsToolAvailableAsync(string toolName)
    {
        var locator = OperatingSystem.IsWindows() ? "where" : "which";

        try
        {
            using var executor = ProcessBuilder
                .For(locator)
                .WithNoWindow()
                .WithArguments(toolName)
                .BuildProcessExecutor();

            var result = await executor.ExecuteBufferedAsync().ConfigureAwait(false);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void AssertConfiguration()
    {
        var validator = new DependencyGeneratorConfigValidator();
        validator.ValidateAndThrow(_configuration);
    }
}