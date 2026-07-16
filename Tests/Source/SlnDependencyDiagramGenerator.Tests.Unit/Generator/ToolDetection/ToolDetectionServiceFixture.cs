using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.ToolDetection;

public class ToolDetectionServiceFixture
{
    private static ToolDetectionService CreateSut()
    {
        return new ToolDetectionService(Substitute.For<IToolPathResolver>(), Substitute.For<ILogger<ToolDetectionService>>());
    }

    private static ToolDetectionService CreateSut(IToolPathResolver resolver)
    {
        return new ToolDetectionService(resolver, Substitute.For<ILogger<ToolDetectionService>>());
    }

    public class CheckToolAvailabilityAsync : ToolDetectionServiceFixture
    {
        [Fact]
        public async Task Should_Return_Available_When_Explicit_Path_Exists()
        {
            var existingPath = typeof(ToolDetectionService).Assembly.Location;
            var resolver = Substitute.For<IToolPathResolver>();
            resolver.GetExplicitPath("d2").Returns(existingPath);
            var service = CreateSut(resolver);

            var result = await service.CheckToolAvailabilityAsync("d2", CancellationToken.None);

            result.IsAvailable.ShouldBeTrue();
            result.ResolvedPath.ShouldBe(existingPath);
        }

        [Fact]
        public async Task Should_Return_Unavailable_When_Explicit_Path_Does_Not_Exist()
        {
            var resolver = Substitute.For<IToolPathResolver>();
            resolver.GetExplicitPath("d2").Returns(@"X:\nonexistent\d2.exe");
            var service = CreateSut(resolver);

            var result = await service.CheckToolAvailabilityAsync("d2", CancellationToken.None);

            result.IsAvailable.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Fall_Back_To_Path_When_No_Explicit_Path()
        {
            var resolver = Substitute.For<IToolPathResolver>();
            resolver.GetExplicitPath("d2").Returns((string?)null);
            var service = CreateSut(resolver);

            var result = await service.CheckToolAvailabilityAsync("d2", CancellationToken.None);

            // PATH check runs 'where d2' / 'which d2' — result depends on whether d2 is installed.
            // We only verify the method completed without throwing and returned a valid result.
            result.ToolName.ShouldBe("d2");
        }
    }

    public class KnownToolNames : ToolDetectionServiceFixture
    {
        [Fact]
        public void Should_Contain_D2()
        {
            var service = CreateSut();

            service.KnownToolNames.ShouldContain("d2");
        }

        [Fact]
        public void Should_Contain_Mmdc()
        {
            var service = CreateSut();

            service.KnownToolNames.ShouldContain("mmdc");
        }

        [Fact]
        public void Should_Have_Exactly_Two_Tools()
        {
            var service = CreateSut();

            service.KnownToolNames.Count.ShouldBe(2);
        }
    }
}