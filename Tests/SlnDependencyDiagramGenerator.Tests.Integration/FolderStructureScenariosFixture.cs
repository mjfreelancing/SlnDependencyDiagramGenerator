using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

public class FolderStructureScenariosFixture
{
    public class OutputStructure : FolderStructureScenariosFixture
    {
        [Fact]
        public async Task Should_Create_One_Output_Folder_Per_Discovered_TargetFramework()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var targetFrameworks = IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot);

            targetFrameworks.ShouldBe(["net10.0", "net8.0", "net9.0"]);
        }

        [Fact]
        public async Task Should_Create_D2_Subfolders_When_D2_Is_The_Only_Configured_Format()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            foreach (var targetFramework in IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot))
            {
                Directory.Exists(Path.Combine(scenarioRun.ExportRoot, targetFramework, "d2")).ShouldBeTrue();
                Directory.Exists(Path.Combine(scenarioRun.ExportRoot, targetFramework, "mmd")).ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Should_Create_Mmd_Subfolders_When_Mermaid_Is_The_Only_Configured_Format()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.Mermaid];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            foreach (var targetFramework in IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot))
            {
                Directory.Exists(Path.Combine(scenarioRun.ExportRoot, targetFramework, "d2")).ShouldBeFalse();
                Directory.Exists(Path.Combine(scenarioRun.ExportRoot, targetFramework, "mmd")).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Should_Always_Create_Summary_File_Regardless_Of_Selected_Diagram_Format()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            foreach (var targetFramework in IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot))
            {
                File.Exists(Path.Combine(scenarioRun.ExportRoot, targetFramework, SummaryDependencyGenerator.MarkdownFilename)).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Should_Clear_Previous_Output_Files_When_ClearContents_Is_Enabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];

            using var tempDirectory = IntegrationTestHarness.CreateTempDirectory("clear-contents");

            var solutionPath = IntegrationTestHarness.GetFixtureSolutionPath("Basic", ".slnx");
            var firstRunConfig = IntegrationTestHarness.CreateConfig(solutionPath, tempDirectory.DirectoryPath, options);
            var firstRunGenerator = new DependencyGenerator(firstRunConfig, NSubstitute.Substitute.For<AllOverIt.Logging.IColorConsoleLogger>());

            await firstRunGenerator.CreateDiagramsAsync();

            var staleFilePath = Path.Combine(tempDirectory.DirectoryPath, "net10.0", "stale.tmp");
            await File.WriteAllTextAsync(staleFilePath, "stale");

            var secondRunConfig = IntegrationTestHarness.CreateConfig(solutionPath, tempDirectory.DirectoryPath, options);
            var secondRunGenerator = new DependencyGenerator(secondRunConfig, NSubstitute.Substitute.For<AllOverIt.Logging.IColorConsoleLogger>());

            await secondRunGenerator.CreateDiagramsAsync();

            File.Exists(staleFilePath).ShouldBeFalse();
        }
    }
}