using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

public class MermaidScenariosFixture
{
    public class MermaidOutput : MermaidScenariosFixture
    {
        [Fact]
        public async Task Should_Omit_Project_Subgraph_When_Grouping_Is_Disabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.Mermaid];
            options.GroupingEnabled = false;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "basic group-all.mmd");

            allDiagram.ShouldNotContain("subgraph basic[\"Basic Group\"]");
        }

        [Fact]
        public async Task Should_Include_Project_Subgraph_When_Grouping_Is_Enabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.Mermaid];
            options.GroupingEnabled = true;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "basic group-all.mmd");

            allDiagram.ShouldContain("subgraph basic[\"Basic Group\"]");
        }

        [Fact]
        public async Task Should_Render_TopToBottom_Direction_As_TB()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.Mermaid];
            options.Direction = GeneratorDiagramOptions.DiagramDirection.TB;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "basic group-all.mmd");

            allDiagram.ShouldContain("flowchart TB");
        }

        [Fact]
        public async Task Should_Apply_Configured_Framework_Style_Fill()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("FrameworkRefs", "Framework Group", "framework");
            options.Formats = [DiagramFormat.Mermaid];
            options.FrameworkFill = "#ABC123";

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "framework group-all.mmd");

            allDiagram.ShouldContain("style microsoft-aspnetcore-app fill:#ABC123");
        }

        [Fact]
        public async Task Should_Omit_Transitive_Edges_When_Depth_Is_Zero()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Transitive", "Transitive Group", "transitive");
            options.Formats = [DiagramFormat.Mermaid];
            options.IndividualTransitiveDepth = 0;
            options.AllTransitiveDepth = 0;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "transitive group-all.mmd");

            allDiagram.ShouldNotContain("microsoft-extensions-http_9-0-0 -->");
        }

        [Fact]
        public async Task Should_Include_Transitive_Edges_When_Depth_Is_Two()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Transitive", "Transitive Group", "transitive");
            options.Formats = [DiagramFormat.Mermaid];
            options.IndividualTransitiveDepth = 2;
            options.AllTransitiveDepth = 2;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "mmd", "transitive group-all.mmd");

            allDiagram.ShouldContain("microsoft-extensions-http_9-0-0 -->");
        }
    }
}