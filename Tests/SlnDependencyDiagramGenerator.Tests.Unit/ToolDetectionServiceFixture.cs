using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class ToolDetectionServiceFixture
{
    public class CheckConfiguredToolsAsync : ToolDetectionServiceFixture
    {
        [Fact]
        public async Task Should_Return_Empty_Statuses_When_No_Image_Formats_Are_Configured()
        {
            var service = new ToolDetectionService();

            var result = await service.CheckConfiguredToolsAsync([], CancellationToken.None);

            result.ToolStatuses.ShouldBeEmpty();
            result.AllRequiredToolsAvailable.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Check_Known_Tools_When_Image_Formats_Are_Configured()
        {
            var service = new ToolDetectionService();

            var result = await service.CheckConfiguredToolsAsync(
                [DiagramImageFormat.Png], CancellationToken.None);

            result.ToolStatuses.Length.ShouldBe(2);
            result.ToolStatuses.ShouldContain(status => status.ToolName == "d2");
            result.ToolStatuses.ShouldContain(status => status.ToolName == "mmdc");
        }
    }
}