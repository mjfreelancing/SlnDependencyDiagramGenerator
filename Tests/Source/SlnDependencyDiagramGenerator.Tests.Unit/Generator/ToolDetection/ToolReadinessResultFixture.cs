using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.ToolDetection;

public class ToolReadinessResultFixture
{
    public class AllRequiredToolsAvailable : ToolReadinessResultFixture
    {
        [Fact]
        public void Should_Be_True_When_No_Tools_Are_Checked()
        {
            var result = new ToolReadinessResult
            {
                ToolStatuses = []
            };

            result.AllRequiredToolsAvailable.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_All_Tools_Are_Available()
        {
            var result = new ToolReadinessResult
            {
                ToolStatuses =
                [
                    new ToolStatus { ToolName = "d2", IsAvailable = true },
                    new ToolStatus { ToolName = "mmdc", IsAvailable = true }
                ]
            };

            result.AllRequiredToolsAvailable.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Any_Tool_Is_Not_Available()
        {
            var result = new ToolReadinessResult
            {
                ToolStatuses =
                [
                    new ToolStatus { ToolName = "d2", IsAvailable = true },
                    new ToolStatus { ToolName = "mmdc", IsAvailable = false }
                ]
            };

            result.AllRequiredToolsAvailable.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_False_When_All_Tools_Are_Not_Available()
        {
            var result = new ToolReadinessResult
            {
                ToolStatuses =
                [
                    new ToolStatus { ToolName = "d2", IsAvailable = false },
                    new ToolStatus { ToolName = "mmdc", IsAvailable = false }
                ]
            };

            result.AllRequiredToolsAvailable.ShouldBeFalse();
        }
    }
}