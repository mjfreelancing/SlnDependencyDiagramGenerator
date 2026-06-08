using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Generator;

public class SummaryScenariosFixture
{
    public class SummaryOutput : SummaryScenariosFixture
    {
        [Fact]
        public async Task Should_Include_Basic_AppConsole_Summary_For_Net10()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldContain("## AppConsole");
            summary.ShouldContain("Dapper v2.1.35");
        }

        [Fact]
        public async Task Should_Only_Emit_Net10_Summary_For_SingleFramework_Fixture()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("SingleFramework", "Single Group", "single");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var targetFrameworks = IntegrationTestHarness.GetTargetFrameworkDirectories(scenarioRun.ExportRoot);

            targetFrameworks.ShouldBe(["net10.0"]);
        }

        [Fact]
        public async Task Should_Include_Conflict_Table_For_Conflicts_Fixture()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Conflicts", "Conflicts Group", "conflicts");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldContain("## Cross-Project Version Conflicts");
            summary.ShouldContain("### Newtonsoft.Json");
            summary.ShouldContain("| LibV1 | 12.0.3 |");
            summary.ShouldContain("| LibV2 | 13.0.3 |");
        }

        [Fact]
        public async Task Should_Include_FrameworkReference_When_Frameworks_Are_Not_Excluded()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("FrameworkRefs", "Framework Group", "framework");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldContain("Microsoft.AspNetCore.App");
        }

        [Fact]
        public async Task Should_Exclude_FrameworkReference_When_Configured()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("FrameworkRefs", "Framework Group", "framework");
            options.FrameworksToExclude = ["Microsoft.AspNetCore.App"];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldNotContain("Microsoft.AspNetCore.App");
        }

        [Fact]
        public async Task Should_Exclude_Configured_Package_From_Summary_Output()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Exclusions", "Exclusions Group", "exclusions");
            options.PackagesToExclude = ["Newtonsoft.Json"];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldNotContain("Newtonsoft.Json");
        }

        [Fact]
        public async Task Should_Exclude_RegexMatched_Project_From_Summary_Output()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Exclusions", "Exclusions Group", "exclusions");
            options.RegexToExclude = [@".*LibExcluded.*\.csproj$"];

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var summary = IntegrationTestHarness.ReadSummaryFile(scenarioRun.ExportRoot, "net10.0");

            summary.ShouldNotContain("## LibExcluded");
        }
    }
}