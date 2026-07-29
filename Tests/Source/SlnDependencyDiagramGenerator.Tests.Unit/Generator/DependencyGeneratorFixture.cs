using AllOverIt.Validation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Tests.Unit.Support;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator;

public class DependencyGeneratorFixture
{
    private readonly IProjectDiscoveryService _projectDiscovery = Substitute.For<IProjectDiscoveryService>();
    private readonly IToolDetectionService _toolDetection = Substitute.For<IToolDetectionService>();
    private readonly IToolPathResolver _toolPathResolver = Substitute.For<IToolPathResolver>();
    private readonly IValidationInvoker _validationInvoker = Substitute.For<IValidationInvoker>();
    private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
    private readonly ILogger<DependencyGenerator> _logger = Substitute.For<ILogger<DependencyGenerator>>();

    public class ValidateConfiguration : DependencyGeneratorFixture
    {
        [Fact]
        public void Should_Delegate_To_ValidationInvoker()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().Build();

            sut.ValidateConfiguration(config);

            _validationInvoker.Received(1).AssertValidation(config);
        }
    }

    public class CreateDiagramsAsync : DependencyGeneratorFixture
    {
        [Fact]
        public async Task Should_Throw_When_Cancellation_Is_Requested_Before_Execution()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().Build();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(() => sut.CreateDiagramsAsync(config, cts.Token));
        }

        [Fact]
        public async Task Should_Throw_ToolNotFoundException_When_Required_Tools_Are_Not_Available()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().WithFormats(DiagramFormat.D2).Build();

            var unavailableStatus = new ToolStatus { ToolName = "d2", IsAvailable = false, ErrorMessage = "d2 not found" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(unavailableStatus));

            var exception = await Should.ThrowAsync<ToolNotFoundException>(() => sut.CreateDiagramsAsync(config, CancellationToken.None));

            exception.Message.ShouldContain("d2");
        }

        [Fact]
        public async Task Should_Not_Throw_When_All_Required_Tools_Are_Available()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().WithFormats(DiagramFormat.D2).Build();

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await Should.NotThrowAsync(
                () => sut.CreateDiagramsAsync(config, CancellationToken.None));
        }

        [Fact]
        public async Task Should_Return_Early_When_No_Target_Frameworks_Are_Discovered()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().Build();

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            _logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("No target frameworks discovered")),
                null,
                Arg.Any<Func<object, Exception?, string>>());

            await _projectDiscovery.DidNotReceive().ParseProjectsAsync(
                Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Report_Discovered_Target_Frameworks()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().Build();

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net8.0", "net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            _logger.Received(1).Log(
                LogLevel.Debug,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Discovered 2 target framework(s)")),
                null,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Should_Discover_Target_Frameworks_With_Correct_Parameters()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder()
                .WithSolutionPath("path/to/test.sln")
                .WithRegexToInclude(".*\\.csproj")
                .WithRegexToExclude(".*Tests\\.csproj")
                .WithFormats(DiagramFormat.D2)
                .Build();

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _projectDiscovery.Received(1).DiscoverTargetFrameworksAsync(
                Arg.Is<string>(path => path.Contains("test.sln")),
                Arg.Is<string[]>(include => include.Length == 1 && include[0] == ".*\\.csproj"),
                Arg.Is<string[]>(exclude => exclude.Length == 1 && exclude[0] == ".*Tests\\.csproj"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Call_ParseProjects_With_SolutionPath_And_Regexes()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder()
                .WithSolutionPath("path/to/test.sln")
                .WithRegexToInclude(".*\\.csproj")
                .WithRegexToExclude(".*Tests\\.csproj")
                .WithFormats(DiagramFormat.D2)
                .Build();

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _projectDiscovery.Received(1).ParseProjectsAsync(
                Arg.Is<SolutionParseRequest>(request =>
                    request.SolutionFilePath == "path/to/test.sln" &&
                    request.RegexToInclude.Length == 1 &&
                    request.RegexToInclude[0] == ".*\\.csproj" &&
                    request.RegexToExclude.Length == 1 &&
                    request.RegexToExclude[0] == ".*Tests\\.csproj"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Use_Disabled_Individual_Depth_As_Zero()
        {
            var sut = CreateSut();

            var config = new TestConfigBuilder().WithFormats(DiagramFormat.D2).Build();
            config.Solution.Individual.Enabled = false;
            config.Solution.All.Enabled = true;
            config.Solution.All.TransitiveDepth = 3;

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _projectDiscovery.Received(1).ParseProjectsAsync(
                Arg.Is<SolutionParseRequest>(request => request.MaxTransitiveDepth == 3),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Use_Max_TransitiveDepth_Across_Scopes()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().WithFormats(DiagramFormat.D2).Build();
            config.Solution.Individual.Enabled = true;
            config.Solution.Individual.TransitiveDepth = 2;
            config.Solution.All.Enabled = true;
            config.Solution.All.TransitiveDepth = 5;

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _projectDiscovery.Received(1).ParseProjectsAsync(
                Arg.Is<SolutionParseRequest>(request => request.MaxTransitiveDepth == 5),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Check_Tools_When_No_Formats_Are_Configured()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().WithFormats().Build();

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _toolDetection.DidNotReceive().CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Check_All_Distinct_Formats_When_Multiple_Configured()
        {
            var sut = CreateSut();
            var config = new TestConfigBuilder().WithFormats(DiagramFormat.D2, DiagramFormat.Mermaid).Build();

            _toolPathResolver.GetToolName(DiagramFormat.D2).Returns("d2");
            _toolPathResolver.GetToolName(DiagramFormat.Mermaid).Returns("mmdc");

            var availableStatus = new ToolStatus { ToolName = "d2", IsAvailable = true, ResolvedPath = "/usr/bin/d2" };

            _toolDetection
                .CheckToolAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(availableStatus));

            _projectDiscovery
                .DiscoverTargetFrameworksAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(["net10.0"]);

            _projectDiscovery
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(CreateEmptyDiscoveryResult());

            _projectDiscovery
                .ParseProjectsAsync(Arg.Any<SolutionParseRequest>(), Arg.Any<CancellationToken>())
                .Returns([]);

            await sut.CreateDiagramsAsync(config, CancellationToken.None);

            await _toolDetection.Received(1).CheckToolAvailabilityAsync("d2", Arg.Any<CancellationToken>());

            await _toolDetection.Received(1).CheckToolAvailabilityAsync("mmdc", Arg.Any<CancellationToken>());
        }
    }

    private static ProjectDiscoveryResult CreateEmptyDiscoveryResult()
    {
        return new ProjectDiscoveryResult
        {
            AllProjectPaths = [],
            IncludedProjectPaths = [],
            ExcludedProjectPaths = [],
            ImplicitlyExcludedProjectPaths = []
        };
    }

    private DependencyGenerator CreateSut()
    {
        _loggerFactory.CreateLogger<DependencyGenerator>().Returns(_logger);

        return new DependencyGenerator(_projectDiscovery, _toolDetection, _toolPathResolver, _validationInvoker, _loggerFactory);
    }
}