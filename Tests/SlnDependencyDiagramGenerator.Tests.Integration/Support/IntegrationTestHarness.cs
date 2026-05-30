using AllOverIt.Logging;
using NSubstitute;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Parser;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Support;

internal static class IntegrationTestHarness
{
    internal sealed class GeneratorScenarioOptions
    {
        public string FixtureName { get; set; } = "Basic";
        public string SolutionExtension { get; set; } = ".slnx";
        public string GroupName { get; set; } = "Test Group";
        public string GroupNameAlias { get; set; } = "test";
        public GeneratorDiagramOptions.DiagramDirection Direction { get; set; } = GeneratorDiagramOptions.DiagramDirection.LR;
        public bool GroupingEnabled { get; set; } = true;
        public string FrameworkFill { get; set; } = "#102030";
        public string PackageFill { get; set; } = "#405060";
        public string TransitiveFill { get; set; } = "#708090";
        public string GroupFill { get; set; } = "#DDEEFF";
        public double FrameworkOpacity { get; set; } = 0.8;
        public double PackageOpacity { get; set; } = 0.8;
        public double TransitiveOpacity { get; set; } = 0.8;
        public double GroupOpacity { get; set; } = 0.75;
        public DiagramFormat[] Formats { get; set; } = [DiagramFormat.D2, DiagramFormat.Mermaid];
        public bool ClearContents { get; set; } = true;
        public bool IncludeIndividual { get; set; } = true;
        public bool IncludeAll { get; set; } = true;
        public bool IncludeDependencies { get; set; } = true;
        public int IndividualTransitiveDepth { get; set; } = 3;
        public int AllTransitiveDepth { get; set; } = 3;
        public string[] RegexToInclude { get; set; } = [@"^.*\.csproj$"];
        public string[] RegexToExclude { get; set; } = [];
        public string[] PackagesToExclude { get; set; } = [];
        public string[] FrameworksToExclude { get; set; } = [];
    }

    internal sealed class ScenarioRunResult : IDisposable
    {
        private readonly DisposableTempDirectory _tempDirectory;

        public ScenarioRunResult(DisposableTempDirectory tempDirectory)
        {
            _tempDirectory = tempDirectory;
        }

        public string ExportRoot => _tempDirectory.DirectoryPath;

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }
    }

    public static string GetFixtureSolutionPath(string fixtureName, string extension)
    {
        return Path.Combine(FixtureLocator.GetFixtureDirectory(fixtureName), $"{fixtureName}{extension}");
    }

    public static GeneratorScenarioOptions CreateScenarioOptions(string fixtureName, string groupName, string groupNameAlias)
    {
        return new GeneratorScenarioOptions
        {
            FixtureName = fixtureName,
            GroupName = groupName,
            GroupNameAlias = groupNameAlias
        };
    }

    public static async Task<ScenarioRunResult> RunGeneratorAsync(GeneratorScenarioOptions options)
    {
        var tempDirectory = CreateTempDirectory(options.FixtureName.ToLowerInvariant());
        var solutionPath = GetFixtureSolutionPath(options.FixtureName, options.SolutionExtension);
        var configuration = CreateConfig(solutionPath, tempDirectory.DirectoryPath, options);
        var generator = new DependencyGenerator(configuration, Substitute.For<IColorConsoleLogger>());

        await generator.CreateDiagramsAsync();

        return new ScenarioRunResult(tempDirectory);
    }

    public static DependencyGeneratorConfig CreateConfig(string solutionPath, string exportRoot, GeneratorScenarioOptions options)
    {
        return new DependencyGeneratorConfig
        {
            Projects = new GeneratorProjectOptions
            {
                SolutionPath = solutionPath,
                RegexToInclude = options.RegexToInclude,
                RegexToExclude = options.RegexToExclude,
                PackagesToExclude = options.PackagesToExclude,
                FrameworksToExclude = options.FrameworksToExclude,
                Individual = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = options.IncludeIndividual,
                    IncludeDependencies = options.IncludeDependencies,
                    TransitiveDepth = options.IndividualTransitiveDepth
                },
                All = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = options.IncludeAll,
                    IncludeDependencies = options.IncludeDependencies,
                    TransitiveDepth = options.AllTransitiveDepth
                }
            },
            Diagram = new GeneratorDiagramOptions
            {
                Direction = options.Direction,
                GroupName = options.GroupName,
                GroupNameAlias = options.GroupNameAlias,
                FrameworkStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = options.FrameworkFill,
                    Opacity = options.FrameworkOpacity
                },
                PackageStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = options.PackageFill,
                    Opacity = options.PackageOpacity
                },
                TransitiveStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = options.TransitiveFill,
                    Opacity = options.TransitiveOpacity
                },
                Grouping = new GeneratorDiagramOptions.GroupingOptions
                {
                    Enabled = options.GroupingEnabled,
                    BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                    {
                        Fill = options.GroupFill,
                        Opacity = options.GroupOpacity
                    }
                },
                Formats = options.Formats
            },
            Export = new GeneratorExportOptions
            {
                ClearContents = options.ClearContents,
                RootPath = exportRoot,
                ImageFormats = []
            }
        };
    }

    public static string[] GetTargetFrameworkDirectories(string exportRoot)
    {
        return [.. Directory.GetDirectories(exportRoot)
            .Select(directory => Path.GetFileName(directory) ?? string.Empty)
            .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase)];
    }

    public static string ReadSummaryFile(string exportRoot, string targetFramework)
    {
        return File.ReadAllText(Path.Combine(exportRoot, targetFramework, SummaryDependencyGenerator.MarkdownFilename));
    }

    public static string ReadDiagramFile(string exportRoot, string targetFramework, string formatFolder, string fileName)
    {
        return File.ReadAllText(Path.Combine(exportRoot, targetFramework, formatFolder, fileName));
    }

    public static string[] GetDiagramFiles(string exportRoot, string targetFramework, string formatFolder, string extension)
    {
        var folderPath = Path.Combine(exportRoot, targetFramework, formatFolder);

        return [.. Directory.GetFiles(folderPath, $"*.{extension}")
            .Select(fileName => Path.GetFileName(fileName) ?? string.Empty)
            .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)];
    }

    public static async Task<SolutionProject[]> ParseFixtureAsync(string fixtureName, string extension, string targetFramework,
        string[] regexToInclude, string[] regexToExclude, string[] excludePackages, string[] excludeFrameworks, int maxTransitiveDepth)
    {
        MsBuildSdkResolver.EnsureInitialized();

        var parser = new SolutionParser();
        var solutionPath = GetFixtureSolutionPath(fixtureName, extension);

        return await parser.ParseAsync(solutionPath, regexToInclude, regexToExclude, excludePackages, excludeFrameworks,
            targetFramework, maxTransitiveDepth);
    }

    public static async Task<string[]> DiscoverFixtureTargetFrameworksAsync(string fixtureName, string extension,
        string[] regexToInclude, string[] regexToExclude)
    {
        MsBuildSdkResolver.EnsureInitialized();

        var parser = new SolutionParser();
        var solutionPath = GetFixtureSolutionPath(fixtureName, extension);

        return await parser.DiscoverTargetFrameworksAsync(solutionPath, regexToInclude, regexToExclude);
    }

    public static DependencyGenerator CreateGenerator(string solutionPath, string exportRoot, string groupName, string groupAlias)
    {
        var options = new GeneratorScenarioOptions
        {
            GroupName = groupName,
            GroupNameAlias = groupAlias
        };

        var configuration = CreateConfig(solutionPath, exportRoot, options);

        return new DependencyGenerator(configuration, Substitute.For<IColorConsoleLogger>());
    }

    public static DisposableTempDirectory CreateTempDirectory(string name)
    {
        return new DisposableTempDirectory(name);
    }
}

internal sealed class DisposableTempDirectory : IDisposable
{
    public DisposableTempDirectory(string name)
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "SlnDependencyDiagramGenerator.Tests.Integration", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}