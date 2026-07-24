using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Run;
using SlnDependencyStudio.Wpf.Features.Solution;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Run;

[Collection(nameof(ReactiveUIInitializer))]
public class PreGenerationAnalysisServiceFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IProjectDiscoveryService _discoveryService = Substitute.For<IProjectDiscoveryService>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IToolStatusService _toolStatus = Substitute.For<IToolStatusService>();
    private readonly ILogger<PreGenerationAnalysisService> _logger = NullLogger<PreGenerationAnalysisService>.Instance;
    private readonly ISolutionOptionsEditor _solutionEditor = Substitute.For<ISolutionOptionsEditor>();
    private readonly PreGenerationAnalysisService _service;

    public PreGenerationAnalysisServiceFixture()
    {
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.GetService(typeof(IProjectDiscoveryService)).Returns(_discoveryService);

        var discoveryFactory = new ScopedOperationFactory<IProjectDiscoveryService>(_scopeFactory);

        _service = new PreGenerationAnalysisService(_store, discoveryFactory, _toolStatus, _logger);
    }

    public class RunAsync : PreGenerationAnalysisServiceFixture
    {
        [Fact]
        public async Task Should_Emit_Error_When_SolutionPath_Empty()
        {
            _store.SolutionOptionsEditor.Returns(_solutionEditor);
            _solutionEditor.SolutionPath.Returns(new Wpf.Controls.TrackableValue<string>());

            var messages = await _service.RunAsync(CancellationToken.None).ToList();

            messages.Count.ShouldBe(2);
            messages[0].Text.ShouldBe("=== Dry-Run Analysis ===");
            messages[1].Text.ShouldBe("No solution path configured");
            messages[1].Level.ShouldBe(OutputMessageLevel.Error);
        }

        [Fact]
        public async Task Should_Emit_Project_Sections_On_Success()
        {
            SetupValidConfiguration(@"C:\path\to\solution.sln");
            SetupDiscoveryResult();

            var messages = await _service.RunAsync(CancellationToken.None).ToList();

            messages.Any(message => message.Text.StartsWith("Projects Discovered:")).ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Resolve_Relative_Path_Against_Project_Directory()
        {
            _store.DocumentFilePath.Returns(@"C:\Projects\myproject.sds");
            SetupValidConfiguration(@"..\solution.sln");
            SetupDiscoveryResult();
            SetupToolStatus();

            await _service.RunAsync(CancellationToken.None);

            await _discoveryService.Received(1).DiscoverProjectsAsync(
                @"C:\solution.sln",
                Arg.Any<string[]>(),
                Arg.Any<string[]>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Resolve_Absolute_Path()
        {
            SetupValidConfiguration(@"C:\absolute\solution.sln");
            SetupDiscoveryResult();
            SetupToolStatus();

            await _service.RunAsync(CancellationToken.None);

            await _discoveryService.Received(1).DiscoverProjectsAsync(
                @"C:\absolute\solution.sln",
                Arg.Any<string[]>(),
                Arg.Any<string[]>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Use_Default_Include_Regex_When_None_Configured()
        {
            SetupValidConfiguration(@"C:\path\to\solution.sln");
            SetupDiscoveryResult();

            await _service.RunAsync(CancellationToken.None);

            var expectedRegex = new[] { ".*\\.csproj" };

            await _discoveryService.Received(1).DiscoverProjectsAsync(
                Arg.Any<string>(),
                Arg.Is<string[]>(regex => regex.Length == 1 && regex[0] == expectedRegex[0]),
                Arg.Any<string[]>(),
                Arg.Any<CancellationToken>());

            _scopeFactory.Received(1).CreateScope();
        }

        [Fact]
        public async Task Should_Emit_Cancellation_When_Cancelled()
        {
            SetupValidConfiguration(@"C:\path\to\solution.sln");

            _discoveryService
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromException<ProjectDiscoveryResult>(new OperationCanceledException()));

            var messages = await _service.RunAsync(CancellationToken.None).ToList();

            messages.Any(message => message.Text == "Analysis cancelled").ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Emit_Error_When_Discovery_Throws()
        {
            SetupValidConfiguration(@"C:\path\to\solution.sln");

            _discoveryService
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromException<ProjectDiscoveryResult>(new InvalidOperationException("Test failure")));

            var messages = await _service.RunAsync(CancellationToken.None).ToList();

            messages.Any(message => message.Text.Contains("Analysis failed")).ShouldBeTrue();
        }
    }

    private void SetupValidConfiguration(string solutionPath)
    {
        var solutionPathTrackable = new Wpf.Controls.TrackableValue<string>();
        solutionPathTrackable.Value = solutionPath;
        solutionPathTrackable.SetOriginalValue(solutionPath);

        _solutionEditor.SolutionPath.Returns(solutionPathTrackable);

        _solutionEditor.RegexToInclude.Returns(new Wpf.Controls.TrackableCollection<string>());
        _solutionEditor.RegexToExclude.Returns(new Wpf.Controls.TrackableCollection<string>());

        _store.SolutionOptionsEditor.Returns(_solutionEditor);
    }

    private void SetupDiscoveryResult()
    {
        _discoveryService
            .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(CreateDiscoveryResult());
    }

    private static ProjectDiscoveryResult CreateDiscoveryResult()
    {
        return new ProjectDiscoveryResult
        {
            AllProjectPaths = ["/src/ProjectA.csproj", "/src/ProjectB.csproj"],
            IncludedProjectPaths = ["/src/ProjectA.csproj", "/src/ProjectB.csproj"],
            ExcludedProjectPaths = [],
            ImplicitlyExcludedProjectPaths = []
        };
    }

    private void SetupToolStatus()
    {
        _toolStatus.ToolStatuses.Returns(
            System.Reactive.Linq.Observable.Return(
                new List<ToolStatusEntry>
                {
                    new() { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" },
                    new() { ToolName = "mmdc", IsAvailable = false }
                } as IReadOnlyList<ToolStatusEntry>));
    }
}
