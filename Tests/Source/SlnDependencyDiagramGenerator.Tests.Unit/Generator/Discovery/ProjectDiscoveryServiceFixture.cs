using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.Discovery;

public class ProjectDiscoveryServiceFixture
{
    private readonly ISolutionParser _solutionParser = Substitute.For<ISolutionParser>();

    private static SolutionProjectDescriptor CreateDescriptor(string projectName, string absolutePath)
    {
        return new SolutionProjectDescriptor(projectName, absolutePath);
    }

    public class DiscoverProjectsAsync : ProjectDiscoveryServiceFixture
    {
        [Fact]
        public async Task Should_Throw_When_Cancellation_Is_Requested_Before_Execution()
        {
            var sut = CreateSut();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(
                () => sut.DiscoverProjectsAsync("test.sln", [], [], cts.Token));
        }

        [Fact]
        public async Task Should_Call_Parser_And_Return_Mapped_Result()
        {
            var sut = CreateSut();

            var allProjects = new[]
            {
                CreateDescriptor("LibA", "/src/LibA/LibA.csproj"),
                CreateDescriptor("App", "/src/App/App.csproj"),
                CreateDescriptor("Tests", "/src/Tests/Tests.csproj")
            };

            var included = allProjects.Take(2).ToArray();
            var excluded = new[] { allProjects[2] };
            var implicitlyExcluded = Array.Empty<SolutionProjectDescriptor>();

            var filtered = CreateFilteredProjects(allProjects, included, excluded, implicitlyExcluded);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            var result = await sut.DiscoverProjectsAsync("test.sln", [".*"], [".*Tests.*"], CancellationToken.None);

            result.AllProjectPaths.Length.ShouldBe(3);
            result.IncludedProjectPaths.Length.ShouldBe(2);
            result.ExcludedProjectPaths.Length.ShouldBe(1);
            result.ImplicitlyExcludedProjectPaths.Length.ShouldBe(0);
        }

        [Fact]
        public async Task Should_Cache_Result_When_Same_Parameters_Are_Used()
        {
            var sut = CreateSut();
            var filtered = CreateFilteredProjects([], [], [], []);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            await sut.DiscoverProjectsAsync("test.sln", [".*"], [], CancellationToken.None);
            await sut.DiscoverProjectsAsync("test.sln", [".*"], [], CancellationToken.None);

            // Parser should only be called once — second call hits the cache.
            await _solutionParser.Received(1).DiscoverProjectsAsync(
                Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Bypass_Cache_When_Solution_Path_Changes()
        {
            var sut = CreateSut();
            var filtered = CreateFilteredProjects([], [], [], []);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            await sut.DiscoverProjectsAsync("a.sln", [".*"], [], CancellationToken.None);
            await sut.DiscoverProjectsAsync("b.sln", [".*"], [], CancellationToken.None);

            await _solutionParser.Received(2).DiscoverProjectsAsync(
                Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Bypass_Cache_When_Include_Regex_Changes()
        {
            var sut = CreateSut();
            var filtered = CreateFilteredProjects([], [], [], []);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            await sut.DiscoverProjectsAsync("test.sln", [".*"], [], CancellationToken.None);
            await sut.DiscoverProjectsAsync("test.sln", [".*\\.csproj"], [], CancellationToken.None);

            await _solutionParser.Received(2).DiscoverProjectsAsync(
                Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Bypass_Cache_When_Exclude_Regex_Changes()
        {
            var sut = CreateSut();
            var filtered = CreateFilteredProjects([], [], [], []);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            await sut.DiscoverProjectsAsync("test.sln", [".*"], [], CancellationToken.None);
            await sut.DiscoverProjectsAsync("test.sln", [".*"], [".*Tests.*"], CancellationToken.None);

            await _solutionParser.Received(2).DiscoverProjectsAsync(
                Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        }
    }

    public class ParseProjectsAsync : ProjectDiscoveryServiceFixture
    {
        [Fact]
        public async Task Should_Delegate_To_Parser_With_Filtered_Projects()
        {
            var sut = CreateSut();
            var descriptor = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");
            var filtered = CreateFilteredProjects([descriptor], [descriptor], [], []);

            var expectedProjects = new[]
            {
                new SolutionProject { Name = "LibA", Path = "/src/LibA/LibA.csproj" }
            };

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            _solutionParser
                .BuildParsedProjects(Arg.Any<SolutionParseRequest>(), Arg.Any<IReadOnlyList<SolutionProjectDescriptor>>())
                .Returns(expectedProjects);

            var request = new SolutionParseRequest
            {
                SolutionFilePath = "test.sln",
                RegexToInclude = [".*"],
                RegexToExclude = [],
                ExcludePackages = [],
                ExcludeFrameworks = [],
                TargetFramework = "net10.0",
                MaxTransitiveDepth = 2
            };

            var result = await sut.ParseProjectsAsync(request, CancellationToken.None);

            result.Length.ShouldBe(1);
            result[0].Name.ShouldBe("LibA");

            _solutionParser.Received(1).BuildParsedProjects(
                Arg.Is<SolutionParseRequest>(request => request.TargetFramework == "net10.0" && request.MaxTransitiveDepth == 2),
                Arg.Is<IReadOnlyList<SolutionProjectDescriptor>>(list => list.Count == 1));
        }
    }

    public class DiscoverTargetFrameworksAsync : ProjectDiscoveryServiceFixture
    {
        [Fact]
        public async Task Should_Delegate_To_Parser_With_Included_Projects()
        {
            var sut = CreateSut();
            var includedProject = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");
            var excludedProject = CreateDescriptor("Tests", "/src/Tests/Tests.csproj");

            var filtered = CreateFilteredProjects(
                [includedProject, excludedProject],
                [includedProject],
                [excludedProject],
                []);

            _solutionParser
                .DiscoverProjectsAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(filtered);

            _solutionParser
                .DiscoverTargetFrameworksAsync(Arg.Any<IReadOnlyList<SolutionProjectDescriptor>>(), Arg.Any<CancellationToken>())
                .Returns(["net8.0", "net10.0"]);

            var result = await sut.DiscoverTargetFrameworksAsync("test.sln", [".*"], [], CancellationToken.None);

            result.ShouldBe(["net8.0", "net10.0"]);

            // Only the included project should be passed to the parser, not the excluded one.
            await _solutionParser.Received(1).DiscoverTargetFrameworksAsync(
                Arg.Is<IReadOnlyList<SolutionProjectDescriptor>>(list =>
                    list.Count == 1 && list[0].ProjectName == "LibA"),
                Arg.Any<CancellationToken>());
        }
    }

    private static FilteredSolutionProjects CreateFilteredProjects(SolutionProjectDescriptor[] all,
        SolutionProjectDescriptor[] included, SolutionProjectDescriptor[] excluded, SolutionProjectDescriptor[] implicitlyExcluded)
    {
        return new FilteredSolutionProjects
        {
            AllProjects = all,
            IncludedProjects = included,
            ExcludedProjects = excluded,
            ImplicitlyExcludedProjects = implicitlyExcluded
        };
    }

    private ProjectDiscoveryService CreateSut()
    {
        return new ProjectDiscoveryService(_solutionParser);
    }
}
