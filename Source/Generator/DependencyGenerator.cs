using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.Logging;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using AllOverIt.Validation.Extensions;
using AllOverItDependencyDiagram.Parser;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram.Generator
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
            if (_configuration.Export.ClearContents)
            {
                ClearFolder(_configuration.Export.Path);
            }

            var maxTransitiveDepth = Math.Max(_configuration.Projects.IndividualTransitiveDepth, _configuration.Projects.AllTransitiveDepth);
            var solutionParser = new SolutionParser(_configuration.PackageFeeds, maxTransitiveDepth, _logger);
            var allProjects = await solutionParser.ParseAsync(_configuration.Projects.SolutionPath, _configuration.Projects.RegexToInclude, _configuration.TargetFramework);

            if (allProjects.Count == 0)
            {
                _logger
                    .Write(ConsoleColor.Red, "No projects found in ")
                    .Write(ConsoleColor.Yellow, Path.GetFileName(_configuration.Projects.SolutionPath))
                    .Write(ConsoleColor.Red, " using the regex(es) ")
                    .WriteLine(ConsoleColor.Yellow, string.Join(", ", _configuration.Projects.RegexToInclude));

                return;
            }

            foreach (var project in allProjects)
            {
                LogDependencies(project);
            }

            var solutionProjects = allProjects.ToDictionary(project => project.Name, project => project);

            await ExportAsSummary(_configuration.Export.Path, solutionProjects);
            await ExportAsIndividual(solutionProjects);
            await ExportAsAll(solutionProjects);
        }

        private static void ClearFolder(string exportPath)
        {
            var files = AllOverIt.IO.FileSearch.GetFiles(exportPath, "*.*", AllOverIt.IO.DiskSearchOptions.None);

            foreach (var file in files)
            {
                file.Delete();
            }
        }

        private async Task ExportAsIndividual(IDictionary<string, SolutionProject> solutionProjects)
        {
            foreach (var scopedProject in solutionProjects.Values)
            {
                var d2Content = GenerateD2Content(scopedProject, solutionProjects);

                await CreateD2FileAndImages(scopedProject.Name, d2Content);
            }
        }

        private Task ExportAsAll(IDictionary<string, SolutionProject> solutionProjects)
        {
            var d2Content = GenerateD2Content(solutionProjects);

            return CreateD2FileAndImages($"{_configuration.Diagram.GroupName}-All", d2Content);
        }

        private async Task CreateD2FileAndImages(string projectScope, string d2Content)
        {
            // Create the file and return the fully-qualified file path
            var filePath = await CreateD2FileAsync(d2Content, GetDiagramAliasId(projectScope, false));

            foreach (var format in _configuration.Export.ImageFormats)
            {
                await ExportD2ImageFileAsync(filePath, format);
            }
        }

        private string GenerateD2Content(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("direction: right");
            sb.AppendLine();

            sb.AppendLine($"{_configuration.Diagram.GroupNamePrefix}: {_configuration.Diagram.GroupName}");

            var dependencySet = new HashSet<string>();
            AppendProjectDependencies(solutionProject, solutionProjects, dependencySet, _configuration.Projects.IndividualTransitiveDepth);

            foreach (var dependency in dependencySet)
            {
                sb.AppendLine(dependency);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateD2Content(IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("direction: right");
            sb.AppendLine();

            sb.AppendLine($"{_configuration.Diagram.GroupNamePrefix}: {_configuration.Diagram.GroupName}");

            var dependencySet = new HashSet<string>();

            foreach (var solutionProject in solutionProjects)
            {
                AppendProjectDependencies(solutionProject.Value, solutionProjects, dependencySet, _configuration.Projects.AllTransitiveDepth);
            }

            foreach (var dependency in dependencySet)
            {
                sb.AppendLine(dependency);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private static async Task ExportAsSummary(string exportPath, IDictionary<string, SolutionProject> solutionProjects)
        {
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            var summaryPath = Path.Combine(exportPath, "Dependency Summary.md");

            await File.WriteAllTextAsync(summaryPath, content);
        }

        private void AppendProjectDependencies(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName);

            dependencySet.Add($"{projectAlias}: {projectName}");

            AppendFrameworkDependencies(solutionProject, dependencySet);
            AppendPackageDependencies(solutionProject, dependencySet, maxTransitiveDepth);

            foreach (var project in solutionProject.Dependencies.SelectMany(item => item.ProjectReferences))
            {
                AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, maxTransitiveDepth);

                dependencySet.Add($"{GetProjectAliasId(project)} <- {projectAlias}");
            }
        }

        private void AppendProjectDependenciesRecursively(ProjectReference projectReference, IDictionary<string, SolutionProject> solutionProjects,
            HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            var projectName = GetProjectName(projectReference);
            var projectAlias = GetDiagramAliasId(projectName);

            dependencySet.Add($"{projectAlias}: {projectName}");

            // Add all packages dependencies (recursively) for the current project
            foreach (var package in solutionProjects[projectName].Dependencies.SelectMany(item => item.PackageReferences))
            {
                var added = AppendPackageDependenciesRecursively(package, dependencySet, maxTransitiveDepth);

                if (added)
                {
                    dependencySet.Add($"{GetPackageAliasId(package)} <- {projectAlias}");
                }
            }

            // Add all project dependencies (recursively) for the current project
            foreach (var project in solutionProjects[projectName].Dependencies.SelectMany(item => item.ProjectReferences))
            {
                AppendProjectDependenciesRecursively(project, solutionProjects, dependencySet, maxTransitiveDepth);

                dependencySet.Add($"{GetProjectAliasId(project)} <- {projectAlias}");
            }
        }

        private void AppendFrameworkDependencies(SolutionProject solutionProject, HashSet<string> dependencySet)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName);

            foreach (var frameworkReference in solutionProject.Dependencies.SelectMany(item => item.FrameworkReferences))
            {
                var frameworkAlias = GetFrameworkAliasId(frameworkReference);

                dependencySet.Add($"{frameworkAlias} <- {projectAlias}");

                AddFrameworkStyleFillEntry(dependencySet, frameworkAlias);
            }
        }

        private void AppendPackageDependencies(SolutionProject solutionProject, HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            var projectName = solutionProject.Name;
            var projectAlias = GetDiagramAliasId(projectName);

            foreach (var package in solutionProject.Dependencies.SelectMany(item => item.PackageReferences))
            {
                var added = AppendPackageDependenciesRecursively(package, dependencySet, maxTransitiveDepth);

                if (added)
                {
                    dependencySet.Add($"{GetPackageAliasId(package)} <- {projectAlias}");
                }
            }
        }

        private bool AppendPackageDependenciesRecursively(PackageReference packageReference, HashSet<string> dependencySet, int maxTransitiveDepth)
        {
            if (packageReference.Depth > maxTransitiveDepth)
            {
                return false;
            }

            var packageName = packageReference.Name;
            var packageAlias = GetDiagramPackageAliasId(packageReference);

            dependencySet.Add($"{packageAlias}: {packageName}\\nv{packageReference.Version}");

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
                if (dependencySet.Contains(transitiveStyleFillEntry))
                {
                    dependencySet.Remove(transitiveStyleFillEntry);
                    dependencySet.Remove($"{packageAlias}.style.opacity: {_configuration.Diagram.TransitiveStyle.Opacity}");
                }

                dependencySet.Add(packageStyleFillEntry);
                dependencySet.Add($"{packageAlias}.style.opacity: {_configuration.Diagram.PackageStyle.Opacity}");
            }

            foreach (var package in packageReference.TransitiveReferences)
            {
                var added = AppendPackageDependenciesRecursively(package, dependencySet, maxTransitiveDepth);

                if (added)
                {
                    dependencySet.Add($"{GetPackageAliasId(package)} <- {packageAlias}");
                }
            }

            return true;
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
            return GetDiagramAliasId(GetProjectName(projectReference));
        }

        private static string GetFrameworkAliasId(FrameworkReference frameworkReference)
        {
            return GetDiagramFrameworkAliasId(frameworkReference);
        }

        private static string GetPackageAliasId(PackageReference packageReference)
        {
            return GetDiagramPackageAliasId(packageReference);
        }

        private async Task<string> CreateD2FileAsync(string content, string projectScope)
        {
            var fileName = projectScope.IsNullOrEmpty()
                ? $"{_configuration.Diagram.GroupName.ToLowerInvariant()}-all.d2"
                : $"{projectScope}.d2";

            var d2FilePath = Path.Combine(_configuration.Export.Path, fileName);

            // Showing how to mix AddFormatted() with AddFragment() where the latter
            // is a simple alternative to using string interpolation.
            _logger.Write("{forecolor:white}Creating diagram: ")
                   .Write(ConsoleColor.Yellow, Path.GetFileName(fileName))
                   .Write("{forecolor:white}...");

            File.WriteAllText(d2FilePath, content);

            await ProcessBuilder
                .For("d2.exe")
                .WithArguments("fmt", d2FilePath)
                .BuildProcessExecutor()
                .ExecuteAsync();

            // An example using formatted text
            _logger.WriteLine("{forecolor:green}Done");

            return d2FilePath;
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

        private string GetDiagramAliasId(string alias, bool includeProjectGroupPrefix = true)
        {
            alias = alias.ToLowerInvariant().Replace(".", "-");

            return includeProjectGroupPrefix
                ? $"{_configuration.Diagram.GroupNamePrefix}.{alias}"
                : alias;
        }

        private static string GetDiagramFrameworkAliasId(FrameworkReference frameworkReference)
        {
            return $"{frameworkReference.Name}".Replace(".", "-").ToLowerInvariant();
        }

        private static string GetDiagramPackageAliasId(PackageReference packageReference)
        {
            return $"{packageReference.Name}_{packageReference.Version}".Replace(".", "-").ToLowerInvariant();
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

        private void LogPackageDependencies(SolutionProject solutionProject)
        {
            var sortedPackageDependenies = solutionProject.Dependencies
                .SelectMany(item => GetAllPackageDependencies(item.PackageReferences))
                .Select(item => (item.Name, item.Version))
                .Distinct()                                     // Multiple packages may depend on another common package
                .Order()
                .GroupBy(item => item.Name);

            foreach (var dependency in sortedPackageDependenies)
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