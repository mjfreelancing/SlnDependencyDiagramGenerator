using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline;

[Collection(nameof(ReactiveUIInitializer))]
public class ToolStatusServiceFixture
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IToolDetectionService _detectionService = Substitute.For<IToolDetectionService>();
    private readonly IApplicationSettingsService _applicationSettings = Substitute.For<IApplicationSettingsService>();
    private readonly ApplicationSettings _settings = new();

    protected ToolStatusServiceFixture()
    {
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IToolDetectionService)).Returns(_detectionService);
        _applicationSettings.CurrentSettings.Returns(_settings);

        _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

        _detectionService
            .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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

    public class Construction : ToolStatusServiceFixture
    {
        [Fact]
        public void Should_Seed_Two_Entries()
        {
            using var sut = CreateSut();

            var entries = sut.ToolStatuses.FirstAsync().Wait();

            entries.Count.ShouldBe(2);
            entries.ShouldContain(e => e.ToolName == "d2");
            entries.ShouldContain(e => e.ToolName == "mmdc");
        }

        [Fact]
        public async Task Should_Seed_Entries_With_Default_Values()
        {
            using var sut = CreateSut();

            var entries = await sut.ToolStatuses.FirstAsync();

            entries.Count.ShouldBe(2);

            foreach (var entry in entries)
            {
                entry.IsAvailable.ShouldBeFalse();
                entry.ResolvedPath.ShouldBeNull();
            }
        }
    }

    public class RescanAsync : ToolStatusServiceFixture
    {
        [Fact]
        public async Task Should_Update_Entries_With_Detection_Results()
        {
            _detectionService
                .CheckToolAvailabilityAsync("d2", null, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ToolStatus
                {
                    ToolName = "d2",
                    IsAvailable = true,
                    ResolvedPath = @"C:\tools\d2.exe"
                }));

            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            var d2 = entries.Single(e => e.ToolName == "d2");
            d2.IsAvailable.ShouldBeTrue();
            d2.ResolvedPath.ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public async Task Should_Mark_Unavailable_When_Not_Found()
        {
            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            var mmdc = entries.Single(e => e.ToolName == "mmdc");
            mmdc.IsAvailable.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Pass_Tool_Path_Override_From_Settings()
        {
            _settings.ToolPathOverrides["d2"] = @"C:\custom\d2.exe";

            using var sut = CreateSut();

            // Perform an explicit rescan — the initial fire-and-forget scan may
            // race with this, so clear received calls first.
            _detectionService.ClearReceivedCalls();

            await sut.RescanAsync(CancellationToken.None);

            await _detectionService
                .Received(1)
                .CheckToolAvailabilityAsync("d2", @"C:\custom\d2.exe", Arg.Any<CancellationToken>());
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

            // Wait for the fire-and-forget initial scan to complete.
            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();

            entries.Count.ShouldBe(2);
            entries.Select(e => e.ToolName).ShouldBe(["d2", "mmdc"], ignoreOrder: true);
        }

        [Fact]
        public async Task Should_Add_New_Tool_When_KnownToolNames_Grows()
        {
            _detectionService.KnownToolNames.Returns(["d2"]);

            using var sut = CreateSut();
            await sut.RescanAsync(CancellationToken.None);

            // Now the detection service reports a new tool.
            _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

            await sut.RescanAsync(CancellationToken.None);

            var entries = await sut.ToolStatuses.FirstAsync();
            entries.Count.ShouldBe(2);
            entries.ShouldContain(e => e.ToolName == "mmdc");
        }

        [Fact]
        public async Task Should_Remove_Tool_When_KnownToolNames_Shrinks()
        {
            _detectionService.KnownToolNames.Returns(["d2", "mmdc"]);

            using var sut = CreateSut();
            await sut.RescanAsync(CancellationToken.None);

            // Now only d2 is known.
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

            // Clear calls from the fire-and-forget initial scan.
            _detectionService.ClearReceivedCalls();

            await sut.RescanAsync(CancellationToken.None);

            await _detectionService
                .Received(1)
                .CheckToolAvailabilityAsync("dot", null, Arg.Any<CancellationToken>());

            await _detectionService
                .DidNotReceive()
                .CheckToolAvailabilityAsync("d2", Arg.Any<string?>(), Arg.Any<CancellationToken>());

            await _detectionService
                .DidNotReceive()
                .CheckToolAvailabilityAsync("mmdc", Arg.Any<string?>(), Arg.Any<CancellationToken>());
        }
    }

    private ToolStatusService CreateSut()
    {
        return new ToolStatusService(_scopeFactory, _applicationSettings);
    }
}
