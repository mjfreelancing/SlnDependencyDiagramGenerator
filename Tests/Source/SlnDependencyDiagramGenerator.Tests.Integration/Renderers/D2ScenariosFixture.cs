using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Renderers;

public class D2ScenariosFixture
{
    public class D2Output : D2ScenariosFixture
    {
        [Fact]
        public async Task Should_Omit_Group_Container_When_Grouping_Is_Disabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];
            options.GroupingEnabled = false;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "basic-group-all.d2");

            allDiagram.ShouldNotContain("basic: Basic Group");
        }

        [Fact]
        public async Task Should_Include_Group_Container_When_Grouping_Is_Enabled()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];
            options.GroupingEnabled = true;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "basic-group-all.d2");

            allDiagram.ShouldContain("basic: Basic Group");
        }

        [Fact]
        public async Task Should_Render_TopToBottom_Direction_As_Down()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];
            options.Direction = GeneratorDiagramOptions.DiagramDirection.TB;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "basic-group-all.d2");

            allDiagram.ShouldContain("direction: down");
        }

        [Fact]
        public async Task Should_Apply_Configured_Framework_Style_Fill()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("FrameworkRefs", "Framework Group", "framework");
            options.Formats = [DiagramFormat.D2];
            options.FrameworkFill = "#123ABC";

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "framework-group-all.d2");

            allDiagram.ShouldContain("microsoft-aspnetcore-app.style.fill: \"#123ABC\"");
        }

        [Fact]
        public async Task Should_Apply_Configured_Package_Style_Fill()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2];
            options.PackageFill = "#0A0B0C";

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var net10AllDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "basic-group-all.d2");

            net10AllDiagram.ShouldContain("dapper_2-1-35.style.fill: \"#0A0B0C\"");
        }

        [Fact]
        public async Task Should_Omit_Transitive_Edges_When_Depth_Is_Zero()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Transitive", "Transitive Group", "transitive");
            options.Formats = [DiagramFormat.D2];
            options.IndividualTransitiveDepth = 0;
            options.AllTransitiveDepth = 0;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "transitive-group-all.d2");

            allDiagram.ShouldNotContain("<- microsoft-extensions-http_9-0-0");
        }

        [Fact]
        public async Task Should_Include_Transitive_Edges_When_Depth_Is_Two()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Transitive", "Transitive Group", "transitive");
            options.Formats = [DiagramFormat.D2];
            options.IndividualTransitiveDepth = 2;
            options.AllTransitiveDepth = 2;

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var allDiagram = IntegrationTestHarness.ReadDiagramFile(scenarioRun.ExportRoot, "net10.0", "d2", "transitive-group-all.d2");

            allDiagram.ShouldContain("<- microsoft-extensions-http_9-0-0");
        }
    }
}