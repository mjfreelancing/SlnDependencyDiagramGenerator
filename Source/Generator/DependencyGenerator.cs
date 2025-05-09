﻿using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.IO;
using AllOverIt.Logging;
using AllOverIt.Patterns.Specification.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator
{
    /// <summary>Parses a Visual Studio Solution file to discover the projects it contains. These projects are then filtered based on
    /// one or more regex expressions, allowing for projects to be filtered based on their name or folder location. Each project is
    /// then parsed to discover any dependent <see cref="ProjectReference"/>, explicit and transitive (implicit) <see cref="PackageReference"/>,
    /// and <see cref="FrameworkReference"/> elements.<br/><br/>
    /// The <see cref="PackageReference"/> elements are recursively resolved to a specified depth from one or more nuget sources with the
    /// transitive references reported based on their minimum package version. This opinionated method of reporting is to allow for the detection
    /// of unintentional use of different versions across multiple projects in the solution and their dependencies.<br/><br/>
    /// With all of this information the dependency generator creates a 'Dependency Summary' markdown report, a dependency diagram for each
    /// project as well as the entire solution (for the projects processed) in D2 format as well as one or more of the <c>svg</c>, <c>png</c>,
    /// or <c>pdf</c> formats.<br/><br/>
    /// Refer to <see cref="DependencyGeneratorConfig"/> for more information on the configuration options available.</summary>
    public sealed partial class DependencyGenerator
    {
        private readonly DependencyGeneratorConfig _configuration;
        private readonly IColorConsoleLogger _logger;

        /// <summary>Constructor.</summary>
        /// <param name="configuration">The dependency generator configuration options.</param>
        /// <param name="logger">A console logger that provides progress information during the processing of projects and generation of diagrams.</param>
        public DependencyGenerator(DependencyGeneratorConfig configuration, IColorConsoleLogger logger)
        {
            _configuration = configuration.WhenNotNull();
            _logger = logger.WhenNotNull();

            AssertConfiguration();
        }

        /// <summary>Initiates the process of parsing the solution projects and diagram generation.</summary>
        /// <returns>A <see cref="Task"/> that completes when the diagram generation has completed.</returns>
        public async Task CreateDiagramsAsync()
        {
            var individualTransitiveDepth = _configuration.Projects.Individual.Enabled ? _configuration.Projects.Individual.TransitiveDepth : 0;
            var allTransitiveDepth = _configuration.Projects.All.Enabled ? _configuration.Projects.All.TransitiveDepth : 0;
            var maxTransitiveDepth = Math.Max(individualTransitiveDepth, allTransitiveDepth);

            var solutionParser = new SolutionParser(_configuration.PackageFeeds, maxTransitiveDepth, _logger);

            foreach (var targetFramework in _configuration.TargetFrameworks)
            {
                var regexToInclude = _configuration.Projects.RegexToInclude;
                var regexToExclude = _configuration.Projects.RegexToExclude;

                var allProjects = await solutionParser.ParseAsync(_configuration.Projects.SolutionPath, regexToInclude, regexToExclude, targetFramework);

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
            foreach (var scopedProject in solutionProjects.Values)
            {
                var d2Content = GenerateIndividualProjectD2Content(scopedProject, solutionProjects);

                await CreateD2FileAndImages(targetFramework, exportPath, scopedProject.Name, d2Content).ConfigureAwait(false);

                _logger.WriteLine();
            }
        }

        private async Task ExportAsAll(string targetFramework, string exportPath, IDictionary<string, SolutionProject> solutionProjects)
        {
            var d2Content = GenerateAllProjectsD2Content(solutionProjects);

            await CreateD2FileAndImages(targetFramework, exportPath, $"{_configuration.Diagram.GroupName}-All", d2Content).ConfigureAwait(false);

            _logger.WriteLine();
        }

        private async Task CreateD2FileAndImages(string targetFramework, string exportPath, string projectScope, string d2Content)
        {
            var fileName = GetD2Filename(exportPath, GetDiagramAliasId(projectScope, false));

            await CreateD2FileAsync(targetFramework, fileName, d2Content);

            foreach (var format in _configuration.Export.ImageFormats)
            {
                await ExportD2ImageFileAsync(fileName, format);
            }
        }

        private string GenerateIndividualProjectD2Content(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"direction: {_configuration.Diagram.Direction}".ToLowerInvariant());
            sb.AppendLine();

            sb.AppendLine($"{_configuration.Diagram.GroupNameAlias}: {_configuration.Diagram.GroupName}");

            var packagesWithMultipleVersions = GetDeepOrderedDistinctPackageDependencies(solutionProject, solutionProjects, kvp => kvp.Count() > 1)
                .ToDictionary(kvp => kvp.Key, kvp => GetDiagramPackageGroupId(kvp.Key));

            var dependencySet = new HashSet<string>();

            AppendProjectDependencies(solutionProject, packagesWithMultipleVersions, solutionProjects, dependencySet, _configuration.Projects.Individual.IncludeDependencies, _configuration.Projects.Individual.TransitiveDepth);

            foreach (var dependency in dependencySet)
            {
                sb.AppendLine(dependency);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateAllProjectsD2Content(IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"direction: {_configuration.Diagram.Direction}".ToLowerInvariant());
            sb.AppendLine();

            sb.AppendLine($"{_configuration.Diagram.GroupNameAlias}: {_configuration.Diagram.GroupName}");

            var dependencySet = new HashSet<string>();

            foreach (var solutionProject in solutionProjects)
            {
                var packagesWithMultipleVersions = GetDeepOrderedDistinctPackageDependencies(solutionProject.Value, solutionProjects, kvp => kvp.Count() > 1)
                    .ToDictionary(kvp => kvp.Key, kvp => GetDiagramPackageGroupId(kvp.Key));

                AppendProjectDependencies(solutionProject.Value, packagesWithMultipleVersions, solutionProjects, dependencySet, _configuration.Projects.All.IncludeDependencies, _configuration.Projects.All.TransitiveDepth);
            }

            foreach (var dependency in dependencySet)
            {
                sb.AppendLine(dependency);
            }

            sb.AppendLine();

            return sb.ToString();
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

        private void AppendProjectDependencies(SolutionProject solutionProject, IDictionary<string, string> packagesWithMultipleVersions,
            IDictionary<string, SolutionProject> solutionProjects, HashSet<string> dependencySet, bool includeDependencies, int maxTransitiveDepth)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName, true);

            dependencySet.Add($"{projectAlias}: {projectName}");

            if (includeDependencies)
            {
                AppendProjectFrameworkDependencies(solutionProject, dependencySet);

                AppendProjectPackageDependencies(solutionProject, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);
            }

            AppendProjectProjectReferences(solutionProject, projectAlias, packagesWithMultipleVersions, solutionProjects, dependencySet, includeDependencies, maxTransitiveDepth);
        }

        private void AppendProjectDependenciesRecursively(ProjectReference projectReference, IDictionary<string, string> packagesWithMultipleVersions,
            IDictionary<string, SolutionProject> solutionProjects, HashSet<string> dependencySet, bool includeDependencies, int maxTransitiveDepth)
        {
            var projectName = GetProjectName(projectReference);
            var projectAlias = GetDiagramAliasId(projectName, true);

            dependencySet.Add($"{projectAlias}: {projectName}");

            if (includeDependencies)
            {
                // Add all packages dependencies (recursively) for the current project
                AppendProjectPackageReferences(solutionProjects[projectName], projectAlias, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);
            }

            // Add all project dependencies (recursively) for the current project
            AppendProjectProjectReferences(solutionProjects[projectName], projectAlias, packagesWithMultipleVersions, solutionProjects, dependencySet, includeDependencies, maxTransitiveDepth);
        }

        private void AppendProjectProjectReferences(SolutionProject solutionProject, string projectAlias, IDictionary<string, string> packagesWithMultipleVersions,
            IDictionary<string, SolutionProject> solutionProjects, HashSet<string> dependencySet, bool includeDependencies, int maxTransitiveDepth)
        {
            foreach (var project in solutionProject.Dependencies.SelectMany(item => item.ProjectReferences))
            {
                AppendProjectDependenciesRecursively(project, packagesWithMultipleVersions, solutionProjects, dependencySet, includeDependencies, maxTransitiveDepth);

                dependencySet.Add($"{GetProjectAliasId(project)} <- {projectAlias}");
            }
        }

        private void AppendProjectFrameworkDependencies(SolutionProject solutionProject, HashSet<string> dependencySet)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName, true);

            foreach (var frameworkReference in solutionProject.Dependencies.SelectMany(item => item.FrameworkReferences))
            {
                var frameworkAlias = GetDiagramFrameworkAliasId(frameworkReference);

                dependencySet.Add($"{frameworkAlias} <- {projectAlias}");

                AddFrameworkStyleFillEntry(dependencySet, frameworkAlias);
            }
        }

        private void AppendProjectPackageDependencies(SolutionProject solutionProject, IDictionary<string, string> packagesWithMultipleVersions,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName, true);

            // Add all packages dependencies (recursively) for the current project
            AppendProjectPackageReferences(solutionProject, projectAlias, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);
        }

        private void AppendProjectPackageReferences(SolutionProject solutionProject, string projectAlias, IDictionary<string, string> packagesWithMultipleVersions,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            foreach (var package in solutionProject.Dependencies.SelectMany(item => item.PackageReferences))
            {
                AppendPackageReferenceWithDependencies(package, projectAlias, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);
            }
        }

        private bool AppendPackageDependenciesRecursively(PackageReference packageReference, IDictionary<string, string> packagesWithMultipleVersions,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            if (packageReference.Depth > maxTransitiveDepth)
            {
                return false;
            }

            var packageName = packageReference.Name;
            var packageAlias = GetDiagramPackageAliasId(packageReference, packagesWithMultipleVersions, dependencySet);

            dependencySet.Add($"{packageAlias}: {packageName}\\nv{packageReference.Version}");

            AddOrUpdatePackageReferenceStyle(packageReference, packageAlias, dependencySet);

            foreach (var package in packageReference.TransitiveReferences)
            {
                AppendPackageReferenceWithDependencies(package, packageAlias, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);
            }

            return true;
        }

        private void AppendPackageReferenceWithDependencies(PackageReference package, string parentAlias, IDictionary<string, string> packagesWithMultipleVersions,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            var added = AppendPackageDependenciesRecursively(package, packagesWithMultipleVersions, dependencySet, maxTransitiveDepth);

            if (added)
            {
                var transitivePackageAlias = GetDiagramPackageAliasId(package, packagesWithMultipleVersions, dependencySet);

                dependencySet.Add($"{transitivePackageAlias} <- {parentAlias}");
            }
        }

        private void AddOrUpdatePackageReferenceStyle(PackageReference packageReference, string packageAlias, HashSet<string> dependencySet)
        {
            var transitiveStyleFillEntry = GetTransitiveStyleFillEntry(packageAlias);
            var packageStyleFillEntry = GetPackageStyleFillEntry(packageAlias);

            // The diagram should style package reference over transient reference
            if (packageReference.IsTransitive)
            {
                if (!dependencySet.Contains(packageStyleFillEntry))
                {
                    dependencySet.Add(transitiveStyleFillEntry);
                    dependencySet.Add($"{packageAlias}.style.opacity: {_configuration.Diagram.TransitiveStyle.Opacity}");
                }
            }
            else
            {
#pragma warning disable CA1868 // Unnecessary call to 'Contains(item)'
                if (dependencySet.Contains(transitiveStyleFillEntry))
                {
                    dependencySet.Remove(transitiveStyleFillEntry);
                    dependencySet.Remove($"{packageAlias}.style.opacity: {_configuration.Diagram.TransitiveStyle.Opacity}");
                }
#pragma warning restore CA1868 // Unnecessary call to 'Contains(item)'

                dependencySet.Add(packageStyleFillEntry);
                dependencySet.Add($"{packageAlias}.style.opacity: {_configuration.Diagram.PackageStyle.Opacity}");
            }
        }

        private void AddFrameworkStyleFillEntry(HashSet<string> dependencySet, string frameworkAlias)
        {
            dependencySet.Add($"{frameworkAlias}.style.fill: \"{_configuration.Diagram.FrameworkStyle.Fill}\"");
            dependencySet.Add($"{frameworkAlias}.style.opacity: {_configuration.Diagram.FrameworkStyle.Opacity}");
        }

        private string GetPackageStyleFillEntry(string packageAlias)
        {
            return $"{packageAlias}.style.fill: \"{_configuration.Diagram.PackageStyle.Fill}\"";
        }

        private string GetTransitiveStyleFillEntry(string transitiveAlias)
        {
            return $"{transitiveAlias}.style.fill: \"{_configuration.Diagram.TransitiveStyle.Fill}\"";
        }

        private static string GetProjectName(ProjectReference projectReference)
        {
            return Path.GetFileNameWithoutExtension(projectReference.Path);
        }

        private string GetProjectAliasId(ProjectReference projectReference)
        {
            return GetDiagramAliasId(GetProjectName(projectReference), true);
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

        private static string GetDiagramFrameworkAliasId(FrameworkReference frameworkReference)
        {
            return frameworkReference.Name.Replace(".", "-").ToLowerInvariant();
        }

        private static string GetDiagramPackageAliasId(PackageReference packageReference, IDictionary<string, string> packagesWithMultipleVersions,
            HashSet<string> dependencySet)
        {
            var packageAlias = $"{packageReference.Name}_{packageReference.Version}".Replace(".", "-").ToLowerInvariant();

            if (packagesWithMultipleVersions.TryGetValue(packageReference.Name, out var diagramPackageName))
            {
                var groupName = $"{diagramPackageName}-group";

                packageAlias = $"{groupName}.{packageAlias}";

                var diagramGroupItem = $"{groupName}: \"\"";

                // Ignored if already in the set
                dependencySet.Add(diagramGroupItem);
            }

            return packageAlias;
        }

        private string GetD2Filename(string exportPath, string projectScope)
        {
            var fileName = projectScope.IsNullOrEmpty()
                ? $"{_configuration.Diagram.GroupName.ToLowerInvariant()}-all.d2"
                : $"{projectScope}.d2";

            return Path.Combine(exportPath, fileName);
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
            var imageFileName = Path.ChangeExtension(d2FileName, $"{format}").ToLowerInvariant();

            _logger
                .Write(ConsoleColor.White, "Creating image: ")
                .Write(ConsoleColor.Yellow, Path.GetFileName(imageFileName))
                .WriteLine(ConsoleColor.White, "...");

            // D2 sends all output to stderr so we need to check for "err:" to differentiate it from "success" / "info" messages.
            var d2Process = ProcessBuilder
                .For("d2.exe")
                .WithNoWindow()
                .WithArguments("-l", "elk", d2FileName, imageFileName)
                .WithErrorOutputHandler((sender, eventArgs) =>
                {
                    if (eventArgs.Data is string message)
                    {
                        var consoleColor = message.StartsWith("err:", StringComparison.InvariantCultureIgnoreCase)
                            ? ConsoleColor.Red
                            : ConsoleColor.Green;

                        _logger.WriteLine(consoleColor, $"  {message}");
                    }
                })
                .BuildProcessExecutor();

            _ = await d2Process.ExecuteAsync();

            _logger.WriteLine(ConsoleColor.Green, "  Done");
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

        private void AssertConfiguration()
        {
            var validator = new DependencyGeneratorConfigValidator();
            validator.ValidateAndThrow(_configuration);
        }
    }
}