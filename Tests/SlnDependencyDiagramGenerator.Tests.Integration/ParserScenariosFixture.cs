using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

public class ParserScenariosFixture
{
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
            using var tempDirectory = IntegrationTestHarness.CreateTempDirectory("malformed-sln");
            var malformedSolutionPath = Path.Combine(tempDirectory.DirectoryPath, "Malformed.sln");

            await File.WriteAllTextAsync(malformedSolutionPath, "This is not a valid solution file.");

            var parser = new SolutionParser();
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
            using var tempDirectory = IntegrationTestHarness.CreateTempDirectory("malformed-slnx");
            var malformedSolutionPath = Path.Combine(tempDirectory.DirectoryPath, "Malformed.slnx");

            await File.WriteAllTextAsync(malformedSolutionPath, "<Solution><Project Path='Broken'");

            var parser = new SolutionParser();
            string[] includeRegex = [@"^.*\.csproj$"];
            string[] excludeRegex = [];

            var exception = await Should.ThrowAsync<DependencyGeneratorException>(
                async () => await parser.DiscoverTargetFrameworksAsync(malformedSolutionPath, includeRegex, excludeRegex, CancellationToken.None));

            exception.Message.ShouldContain("Failed to parse solution file");
            exception.Message.ShouldContain("Malformed.slnx");
        }
    }
}