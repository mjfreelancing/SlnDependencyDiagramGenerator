using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline.Services;

[Collection(nameof(ReactiveUIInitializer))]
public class ToolStatusServiceFixture
{
    private readonly IToolDetectionService _detectionService = Substitute.For<IToolDetectionService>();

    protected ToolStatusServiceFixture()
    {
        _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

        _detectionService
            .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var toolName = callInfo.ArgAt<string>(0);
                return Task.FromResult(new ToolStatus
                {
                    ToolName = toolName,
                    IsAvailable = false,
                    ErrorMessage = $"'{toolName}' was not found on PATH."
                });
            });
    }

    public class RescanAsync : ToolStatusServiceFixture
    {
        [Fact]
        public async Task Should_Update_Entries_With_Detection_Results()
        {
            _detectionService
                .CheckToolAvailabilityAsync("d2", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ToolStatus
                {
                    ToolName = "d2",
                    IsAvailable = true,
                    ResolvedPath = @"C:\tools\d2.exe"
                }));

            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            var d2 = entries.Single(entry => entry.ToolName == "d2");
            d2.IsAvailable.ShouldBeTrue();
            d2.ResolvedPath.ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public async Task Should_Mark_Unavailable_When_Not_Found()
        {
            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            var mmdc = entries.Single(entry => entry.ToolName == "mmdc");
            mmdc.IsAvailable.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Set_LastChecked_On_Each_Entry()
        {
            using var sut = CreateSut();

            var before = DateTime.UtcNow;

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            foreach (var entry in entries)
            {
                entry.LastChecked.ShouldBeGreaterThanOrEqualTo(before);
            }
        }

        [Fact]
        public async Task Should_Seed_Entries_From_Detection_Service()
        {
            _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            entries.Count.ShouldBe(2);
            entries.Select(entry => entry.ToolName).ShouldBe(["d2", "mmdc"], ignoreOrder: true);
        }

        [Fact]
        public async Task Should_Add_New_Tool_When_KnownToolNames_Grows()
        {
            _detectionService.KnownToolNames.Returns(["d2"]);

            using var sut = CreateSut();
            await sut.RescanAsync(CancellationToken.None);

            _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();
            entries.Count.ShouldBe(2);
            entries.ShouldContain(entry => entry.ToolName == "mmdc");
        }

        [Fact]
        public async Task Should_Remove_Tool_When_KnownToolNames_Shrinks()
        {
            _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

            using var sut = CreateSut();
            await sut.RescanAsync(CancellationToken.None);

            _detectionService.KnownToolNames.Returns(["d2"]);
            _detectionService.ClearReceivedCalls();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();
            entries.Count.ShouldBe(1);
            entries[0].ToolName.ShouldBe("d2");
        }

        [Fact]
        public async Task Should_Scan_Only_Tools_Reported_By_Detection_Service()
        {
            _detectionService.KnownToolNames.Returns(["dot"]);

            using var sut = CreateSut();

            _detectionService.ClearReceivedCalls();

            await sut.RescanAsync(CancellationToken.None);

            await _detectionService
                .Received(1)
                .CheckToolAvailabilityAsync("dot", Arg.Any<CancellationToken>());

            await _detectionService
                .DidNotReceive()
                .CheckToolAvailabilityAsync("d2", Arg.Any<CancellationToken>());

            await _detectionService
                .DidNotReceive()
                .CheckToolAvailabilityAsync("mmdc", Arg.Any<CancellationToken>());
        }
    }

    private ToolStatusService CreateSut()
    {
        return new ToolStatusService(_detectionService, Substitute.For<ILogger<ToolStatusService>>());
    }
}