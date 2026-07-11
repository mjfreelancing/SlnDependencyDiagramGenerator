using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    private ToolStatusService CreateSut()
    {
        return new ToolStatusService(_scopeFactory, _applicationSettings);
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

            var entries = sut.ToolStatuses.FirstAsync().Wait();

            var d2 = entries.Single(e => e.ToolName == "d2");
            d2.IsAvailable.ShouldBeTrue();
            d2.ResolvedPath.ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public async Task Should_Mark_Unavailable_When_Not_Found()
        {
            using var sut = CreateSut();

            await sut.RescanAsync(CancellationToken.None);

            var entries = sut.ToolStatuses.FirstAsync().Wait();

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

            var entries = sut.ToolStatuses.FirstAsync().Wait();

            foreach (var entry in entries)
            {
                entry.LastChecked.ShouldBeGreaterThanOrEqualTo(before);
            }
        }
    }
}
