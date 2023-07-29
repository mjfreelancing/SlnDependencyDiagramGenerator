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
    public sealed partial class DependencyGenerator
    {
        private readonly DependencyGeneratorConfig _generatorConfig;
        private readonly IColorConsoleLogger _logger;

        public DependencyGenerator(DependencyGeneratorConfig generatorConfig, IColorConsoleLogger logger)
        {
            _generatorConfig = generatorConfig.WhenNotNull();
            _logger = logger.WhenNotNull();

            AssertConfiguration();
        }

        public async Task CreateDiagramsAsync()
        {
            if (_generatorConfig.Export.ClearContents)
            {
                ClearFolder(_generatorConfig.Export.Path);
            }

            var maxTransitiveDepth = Math.Max(_generatorConfig.Projects.IndividualTransitiveDepth, _generatorConfig.Projects.AllTransitiveDepth);
            var solutionParser = new SolutionParser(_generatorConfig.PackageFeeds, maxTransitiveDepth, _logger);
            var allProjects = await solutionParser.ParseAsync(_generatorConfig.Projects.SolutionPath, _generatorConfig.Projects.RegexToInclude, _generatorConfig.TargetFramework);

            if (allProjects.Count == 0)
            {
                _logger
                    .Write(ConsoleColor.Red, "No projects found in ")
                    .Write(ConsoleColor.Yellow, Path.GetFileName(_generatorConfig.Projects.SolutionPath))
                    .Write(ConsoleColor.Red, " using the regex(es) ")
                    .WriteLine(ConsoleColor.Yellow, string.Join(", ", _generatorConfig.Projects.RegexToInclude));

                return;
            }

            foreach (var project in allProjects)
            {
                LogDependencies(project);
            }

            var solutionProjects = allProjects.ToDictionary(project => project.Name, project => project);

            await ExportAsSummary(_generatorConfig.Export.Path, solutionProjects);
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

            return CreateD2FileAndImages($"{_generatorConfig.Diagram.GroupName}-All", d2Content);
        }

        private async Task CreateD2FileAndImages(string projectScope, string d2Content)
        {
            // Create the file and return the fully-qualified file path
            var filePath = await CreateD2FileAsync(d2Content, GetDiagramAliasId(projectScope, false));

            foreach (var format in _generatorConfig.Export.ImageFormats)
            {
                await ExportD2ImageFileAsync(filePath, format);
            }
        }

        private string GenerateD2Content(SolutionProject solutionProject, IDictionary<string, SolutionProject> solutionProjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine("direction: right");
            sb.AppendLine();

            sb.AppendLine($"{_generatorConfig.Diagram.GroupNamePrefix}: {_generatorConfig.Diagram.GroupName}");

            var dependencySet = new HashSet<string>();
            AppendProjectDependencies(solutionProject, solutionProjects, dependencySet, _generatorConfig.Projects.IndividualTransitiveDepth);

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

            sb.AppendLine($"{_generatorConfig.Diagram.GroupNamePrefix}: {_generatorConfig.Diagram.GroupName}");

            var dependencySet = new HashSet<string>();

            foreach (var solutionProject in solutionProjects)
            {
                AppendProjectDependencies(solutionProject.Value, solutionProjects, dependencySet, _generatorConfig.Projects.AllTransitiveDepth);
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
                    dependencySet.Add($"{packageAlias}.style.opacity: {_generatorConfig.Diagram.TransitiveStyle.Opacity}");
                }
            }
            else
            {
                if (dependencySet.Contains(transitiveStyleFillEntry))
                {
                    dependencySet.Remove(transitiveStyleFillEntry);
                    dependencySet.Remove($"{packageAlias}.style.opacity: {_generatorConfig.Diagram.TransitiveStyle.Opacity}");
                }

                dependencySet.Add(packageStyleFillEntry);
                dependencySet.Add($"{packageAlias}.style.opacity: {_generatorConfig.Diagram.PackageStyle.Opacity}");
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
            dependencySet.Add($"{frameworkAlias}.style.fill: \"{_generatorConfig.Diagram.FrameworkStyle.Fill}\"");
            dependencySet.Add($"{frameworkAlias}.style.opacity: {_generatorConfig.Diagram.FrameworkStyle.Opacity}");
        }

        private string GetPackageStyleFillEntry(string packageAlias)
        {
            return $"{packageAlias}.style.fill: \"{_generatorConfig.Diagram.PackageStyle.Fill}\"";
        }

        private string GetTransitiveStyleFillEntry(string transitiveAlias)
        {
            return $"{transitiveAlias}.style.fill: \"{_generatorConfig.Diagram.TransitiveStyle.Fill}\"";
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
                ? $"{_generatorConfig.Diagram.GroupName.ToLowerInvariant()}-all.d2"
                : $"{projectScope}.d2";

            var d2FilePath = Path.Combine(_generatorConfig.Export.Path, fileName);

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
                .Write(ConsoleColor.White, "...");

            var d2Process = ProcessBuilder
               .For("d2.exe")
               .WithNoWindow()
               .WithArguments("-l", "elk", d2FileName, imageFileName)
               .BuildProcessExecutor();

            await d2Process.ExecuteAsync();

            // An example using a foreground color and text
            _logger.WriteLine(ConsoleColor.Green, "Done");
        }

        private string GetDiagramAliasId(string alias, bool includeProjectGroupPrefix = true)
        {
            alias = alias.ToLowerInvariant().Replace(".", "-");

            return includeProjectGroupPrefix
                ? $"{_generatorConfig.Diagram.GroupNamePrefix}.{alias}"
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
            validator.ValidateAndThrow(_generatorConfig);
        }
    }
}