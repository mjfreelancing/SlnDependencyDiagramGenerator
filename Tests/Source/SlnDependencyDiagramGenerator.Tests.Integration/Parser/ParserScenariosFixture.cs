using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using SlnDependencyDiagramGenerator.Tests.Shared;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Parser;

public class ParserScenariosFixture : FixtureCollectionTestBase
{
    public class DiscoverProjects : ParserScenariosFixture
    {
        [Fact]
        public async Task Should_Classify_Projects_When_Exclude_Regex_Removes_One_Project()
        {
            // Baseline projects (Exclusions.slnx): LibA, LibB, LibExcluded.
            // Include regex (^.*\.csproj$) keeps: LibA, LibB, LibExcluded.
            // Exclude regex (^LibExcluded$) removes: LibExcluded.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"^LibExcluded$"];

            var discovery = await IntegrationTestHarness.DiscoverFixtureProjectsAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            discovery.AllProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibA", "LibB", "LibExcluded"]);
            discovery.IncludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibA", "LibB"]);
            discovery.ExcludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibExcluded"]);
            discovery.ImplicitlyExcludedProjectPaths.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Classify_Projects_When_Include_Regex_Selects_Single_Project()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^LibA$) keeps: LibA.
            // Exclude regex (none) removes: nothing.
            string[] includeRegex = [@"^LibA$"];
            string[] excludeRegex = [];

            var discovery = await IntegrationTestHarness.DiscoverFixtureProjectsAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            discovery.AllProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["AppConsole", "LibA", "LibB"]);
            discovery.IncludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibA"]);
            discovery.ExcludedProjectPaths.ShouldBeEmpty();
            discovery.ImplicitlyExcludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["AppConsole", "LibB"]);
        }

        [Fact]
        public async Task Should_Classify_Projects_When_Include_And_Exclude_Regexes_Both_Apply()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^Lib.*$) keeps: LibA, LibB.
            // Exclude regex (^LibB$) removes: LibB.
            string[] includeRegex = [@"^Lib.*$"];
            string[] excludeRegex = [@"^LibB$"];

            var discovery = await IntegrationTestHarness.DiscoverFixtureProjectsAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            discovery.AllProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["AppConsole", "LibA", "LibB"]);
            discovery.IncludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibA"]);
            discovery.ExcludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["LibB"]);
            discovery.ImplicitlyExcludedProjectPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name).ShouldBe(["AppConsole"]);
        }
    }

    public class DiscoverTargetFrameworks : ParserScenariosFixture
    {
        [Fact]
        public async Task Should_Discover_Frameworks_For_Basic_Sln()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var targetFrameworks = await IntegrationTestHarness.DiscoverFixtureTargetFrameworksAsync(
                fixtureName: "Basic",
                extension: ".sln",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            targetFrameworks.ShouldBe(["net8.0", "net9.0", "net10.0"]);
        }

        [Fact]
        public async Task Should_Discover_Frameworks_For_Basic_Slnx()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var targetFrameworks = await IntegrationTestHarness.DiscoverFixtureTargetFrameworksAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            targetFrameworks.ShouldBe(["net8.0", "net9.0", "net10.0"]);
        }

        [Fact]
        public async Task Should_Discover_Only_Net10_For_SingleFramework_Fixture()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var targetFrameworks = await IntegrationTestHarness.DiscoverFixtureTargetFrameworksAsync(
                fixtureName: "SingleFramework",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            targetFrameworks.ShouldBe(["net10.0"]);
        }

        [Fact]
        public async Task Should_Exclude_Frameworks_From_RegexExcluded_Projects()
        {
            // Baseline projects (Exclusions.slnx): LibA, LibB, LibExcluded.
            // Include regex (^.*\.csproj$) keeps: LibA, LibB, LibExcluded.
            // Exclude regex (.*LibExcluded.*\.csproj$) removes: LibExcluded.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@".*LibExcluded.*\.csproj$"];

            var targetFrameworks = await IntegrationTestHarness.DiscoverFixtureTargetFrameworksAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex);

            targetFrameworks.ShouldBe(["net10.0"]);
        }
    }

    public class Parse : ParserScenariosFixture
    {
        [Fact]
        public async Task Should_Parse_Basic_Projects_For_Net10_From_Sln()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".sln",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["AppConsole", "LibA", "LibB"]);
        }

        [Fact]
        public async Task Should_Parse_Basic_Projects_For_Net10_From_Slnx()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["AppConsole", "LibA", "LibB"]);
        }

        [Fact]
        public async Task Should_Include_Single_Project_When_Include_Regex_Matches_Exact_Project_Name()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^LibA$) keeps: LibA.
            // Exclude regex (none) removes: nothing.
            string[] includeRegex = [@"^LibA$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibA"]);
        }

        [Fact]
        public async Task Should_Exclude_Single_Project_When_Exclude_Regex_Matches_Exact_Project_Name()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^.*\.csproj$) keeps: AppConsole, LibA, LibB.
            // Exclude regex (^LibB$) removes: LibB.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"^LibB$"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["AppConsole", "LibA"]);
        }

        [Fact]
        public async Task Should_Exclude_Single_Project_When_Exclude_Regex_Matches_Exact_Project_File_Name()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^.*\.csproj$) keeps: AppConsole, LibA, LibB.
            // Exclude regex (^LibB\.csproj$) removes: LibB.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"^LibB\.csproj$"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["AppConsole", "LibA"]);
        }

        [Fact]
        public async Task Should_Include_Single_Project_When_Include_Regex_Matches_Exact_Project_File_Name()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regex (^LibB\.csproj$) keeps: LibB.
            // Exclude regex (none) removes: nothing.
            string[] includeRegex = [@"^LibB\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibB"]);
        }

        [Fact]
        public async Task Should_Exclude_Single_Project_When_Exclude_Regex_Matches_Folder_Path_With_Multiple_Projects()
        {
            // Baseline projects (Exclusions.slnx): LibA, LibB, LibExcluded.
            // Include regex (^.*\.csproj$) keeps: LibA, LibB, LibExcluded.
            // Exclude regex ([/\\]Exclusions[/\\]LibExcluded[/\\]) removes: LibExcluded.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"[/\\]Exclusions[/\\]LibExcluded[/\\]"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibA", "LibB"]);
        }

        [Fact]
        public async Task Should_Exclude_Single_Project_When_Exclude_Regex_Matches_Exact_Project_Name_With_Multiple_Projects()
        {
            // Baseline projects (Exclusions.slnx): LibA, LibB, LibExcluded.
            // Include regex (^.*\.csproj$) keeps: LibA, LibB, LibExcluded.
            // Exclude regex (^LibExcluded$) removes: LibExcluded.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"^LibExcluded$"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibA", "LibB"]);
        }

        [Fact]
        public async Task Should_Include_Projects_When_Any_Include_Regex_Matches_With_Multiple_Patterns()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regexes (^LibA$, ^LibB$) keep: LibA, LibB.
            // Exclude regex (none) removes: nothing.
            string[] includeRegex = [@"^LibA$", @"^LibB$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibA", "LibB"]);
        }

        [Fact]
        public async Task Should_Exclude_Projects_When_Any_Exclude_Regex_Matches_With_Multiple_Patterns()
        {
            // Baseline projects (Exclusions.slnx): LibA, LibB, LibExcluded.
            // Include regex (^.*\.csproj$) keeps: LibA, LibB, LibExcluded.
            // Exclude regexes (^LibA$, ^LibExcluded$) remove: LibA, LibExcluded.
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [@"^LibA$", @"^LibExcluded$"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibB"]);
        }

        [Fact]
        public async Task Should_Apply_Multiple_Include_And_Exclude_Regexes_Together()
        {
            // Baseline projects (Basic.slnx): AppConsole, LibA, LibB.
            // Include regexes (^Lib.*$, ^AppConsole$) keep: AppConsole, LibA, LibB.
            // Exclude regexes (^LibB$, ^AppConsole$) remove: AppConsole, LibB.
            string[] includeRegex = [@"^Lib.*$", @"^AppConsole$"];
            string[] excludeRegex = [@"^LibB$", @"^AppConsole$"];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            projects.Select(project => project.Name).ShouldBe(["LibA"]);
        }

        [Fact]
        public async Task Should_Include_AspNetCore_FrameworkReference_When_Not_Excluded()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "FrameworkRefs",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            var webLib = projects.Single(project => project.Name == "WebLib");
            webLib.FrameworkReferences.Any(framework => framework.Name == "Microsoft.AspNetCore.App").ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Exclude_FrameworkReferences_When_Configured()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = ["Microsoft.AspNetCore.App"];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "FrameworkRefs",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            var webLib = projects.Single(project => project.Name == "WebLib");
            webLib.FrameworkReferences.Any(framework => framework.Name == "Microsoft.AspNetCore.App").ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Classify_Explicit_And_Transitive_Packages_By_Depth()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Transitive",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            var coreLib = projects.Single(project => project.Name == "CoreLib");
            coreLib.PackageReferences.Any(package => package.IsTransitive).ShouldBeFalse();
            coreLib.PackageReferences.Single().TransitiveReferences.Any(transitivePackage => transitivePackage.IsTransitive).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Exclude_Configured_Packages_From_Project_Packages()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = ["Newtonsoft.Json"];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Exclusions",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            var libA = projects.Single(project => project.Name == "LibA");
            libA.PackageReferences.Any(package => package.Name == "Newtonsoft.Json").ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Parse_ProjectReference_Chain_For_AppConsole()
        {
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];
            string[] excludePackages = [];
            string[] excludeFrameworks = [];

            var projects = await IntegrationTestHarness.ParseFixtureAsync(
                fixtureName: "Basic",
                extension: ".slnx",
                targetFramework: "net10.0",
                regexToInclude: includeRegex,
                regexToExclude: excludeRegex,
                excludePackages: excludePackages,
                excludeFrameworks: excludeFrameworks,
                maxTransitiveDepth: 3);

            var appConsole = projects.Single(project => project.Name == "AppConsole");
            appConsole.ProjectReferences.Length.ShouldBe(2);
            appConsole.ProjectReferences.Any(projectReference => projectReference.Path.EndsWith("LibA.csproj", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
            appConsole.ProjectReferences.Any(projectReference => projectReference.Path.EndsWith("LibB.csproj", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Throw_Clear_Error_For_Malformed_Sln_File()
        {
            using var tempDirectory = new DisposableTempDirectory("malformed-sln");
            var malformedSolutionPath = Path.Combine(tempDirectory.DirectoryPath, "Malformed.sln");

            await File.WriteAllTextAsync(malformedSolutionPath, "This is not a valid solution file.", TestContext.Current.CancellationToken);

            var parser = new SolutionParser(
                new ProjectAssetReader(NullLogger<ProjectAssetReader>.Instance),
                [new SlnSolutionProjectResolver(), new SlnxSolutionProjectResolver()],
                NullLogger<SolutionParser>.Instance);
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var exception = await Should.ThrowAsync<DependencyGeneratorException>(
                async () => await parser.DiscoverTargetFrameworksAsync(malformedSolutionPath, includeRegex, excludeRegex, CancellationToken.None));

            exception.Message.ShouldContain("Failed to parse solution file");
            exception.Message.ShouldContain("Malformed.sln");
        }

        [Fact]
        public async Task Should_Throw_Clear_Error_For_Malformed_Slnx_File()
        {
            using var tempDirectory = new DisposableTempDirectory("malformed-slnx");
            var malformedSolutionPath = Path.Combine(tempDirectory.DirectoryPath, "Malformed.slnx");

            await File.WriteAllTextAsync(malformedSolutionPath, "<Solution><Project Path='Broken'", TestContext.Current.CancellationToken);

            var parser = new SolutionParser(
                new ProjectAssetReader(NullLogger<ProjectAssetReader>.Instance),
                [new SlnSolutionProjectResolver(), new SlnxSolutionProjectResolver()],
                NullLogger<SolutionParser>.Instance);
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var exception = await Should.ThrowAsync<DependencyGeneratorException>(
                async () => await parser.DiscoverTargetFrameworksAsync(malformedSolutionPath, includeRegex, excludeRegex, CancellationToken.None));

            exception.Message.ShouldContain("Failed to parse solution file");
            exception.Message.ShouldContain("Malformed.slnx");
        }
    }
}