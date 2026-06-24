using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.ToolDetection;

public class ToolDetectionServiceFixture
{
    public class CheckConfiguredToolsAsync : ToolDetectionServiceFixture
    {
        [Fact]
        public async Task Should_Return_Empty_Statuses_When_No_Image_Formats_Are_Configured()
        {
            var service = new ToolDetectionService(Substitute.For<ILogger<ToolDetectionService>>());

            var result = await service.CheckConfiguredToolsAsync([], CancellationToken.None);

            result.ToolStatuses.ShouldBeEmpty();
            result.AllRequiredToolsAvailable.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Check_Known_Tool_When_A_Diagram_Format_Is_Configured()
        {
            var service = new ToolDetectionService(Substitute.For<ILogger<ToolDetectionService>>());

            var result = await service.CheckConfiguredToolsAsync([DiagramFormat.D2], CancellationToken.None);

            result.ToolStatuses.Length.ShouldBe(1);
            result.ToolStatuses.ShouldContain(status => status.ToolName == "d2");
        }

        [Fact]
        public async Task Should_Check_All_Known_Tools_When_All_Diagram_Formats_Are_Configured()
        {
            var service = new ToolDetectionService(Substitute.For<ILogger<ToolDetectionService>>());

            var result = await service.CheckConfiguredToolsAsync([DiagramFormat.D2, DiagramFormat.Mermaid], CancellationToken.None);

            result.ToolStatuses.Length.ShouldBe(2);
            result.ToolStatuses.ShouldContain(status => status.ToolName == "d2");
            result.ToolStatuses.ShouldContain(status => status.ToolName == "mmdc");
        }
    }
}