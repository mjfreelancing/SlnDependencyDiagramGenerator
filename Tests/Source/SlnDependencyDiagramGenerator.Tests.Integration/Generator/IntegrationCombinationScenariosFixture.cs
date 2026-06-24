using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Generator;

public class IntegrationCombinationScenariosFixture
{
    public class CombinedOutputModes : IntegrationCombinationScenariosFixture
    {
        [Fact]
        public async Task Should_Emit_Only_Individual_Diagrams_When_All_Scope_Is_Disabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.IncludeAll = false;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var snapshot = IntegrationTestHarness.CollectExportSnapshot(scenarioRun.ExportRoot);

            snapshot.Keys.ShouldContain(key => key.EndsWith("/d2/appconsole.d2", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldContain(key => key.EndsWith("/mmd/appconsole.mmd", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldNotContain(key => key.Contains("-group-all.d2", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldNotContain(key => key.Contains("-group-all.mmd", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Should_Emit_Only_All_Diagrams_When_Individual_Scope_Is_Disabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.IncludeIndividual = false;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var snapshot = IntegrationTestHarness.CollectExportSnapshot(scenarioRun.ExportRoot);

            snapshot.Keys.ShouldContain(key => key.EndsWith("/d2/basic-group-all.d2", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldContain(key => key.EndsWith("/mmd/basic-group-all.mmd", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldNotContain(key => key.EndsWith("/d2/appconsole.d2", StringComparison.OrdinalIgnoreCase));
            snapshot.Keys.ShouldNotContain(key => key.EndsWith("/mmd/appconsole.mmd", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Should_Support_EndToEnd_Generation_From_Sln_Files()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.SolutionExtension = ".sln";

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var targetFrameworks = IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot);

            targetFrameworks.ShouldBe(["net10.0", "net8.0", "net9.0"]);
            IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0").ShouldContain("## AppConsole");
        }

        [Fact]
        public async Task Should_Use_Sanitized_AllScope_File_Names_For_Complex_Group_Names()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Complex Group ! 2026 / Demo", "complex");
            options.Formats = [DiagramFormat.D2, DiagramFormat.Mermaid];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var d2Files = IntegrationTestHarness.GetDiagramFiles(scenarioRun.ExportRoot, "net10.0", "d2", "d2");
            var mermaidFiles = IntegrationTestHarness.GetDiagramFiles(scenarioRun.ExportRoot, "net10.0", "mmd", "mmd");

            d2Files.ShouldContain("complex-group-2026-demo-all.d2");
            mermaidFiles.ShouldContain("complex-group-2026-demo-all.mmd");
        }

        [Fact]
        public async Task Should_Allow_Different_Transitive_Depths_Between_All_And_Individual_Scopes()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Transitive", "Transitive Group", "transitive");
            options.Formats = [DiagramFormat.D2];
            options.IndividualTransitiveDepth = 0;
            options.AllTransitiveDepth = 2;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "transitive-group-all.d2");
            var individualFiles = IntegrationTestHarness.GetDiagramFiles(scenarioRun.ExportRoot, "net10.0", "d2", "d2")
                .Where(fileName => !fileName.EndsWith("-group-all.d2", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            allDiagram.ShouldContain("<- microsoft-extensions-http_9-0-0");

            foreach (var individualFile in individualFiles)
            {
                var individualDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", individualFile);
                individualDiagram.ShouldNotContain("<- microsoft-extensions-http_9-0-0");
            }
        }
    }
}