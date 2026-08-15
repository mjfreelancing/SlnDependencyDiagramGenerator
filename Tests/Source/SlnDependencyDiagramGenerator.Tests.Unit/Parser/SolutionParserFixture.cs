using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Parser;

public class SolutionParserFixture
{
    private readonly IProjectAssetReader _assetReader = Substitute.For<IProjectAssetReader>();
    private readonly ISolutionProjectResolver _slnResolver = Substitute.For<ISolutionProjectResolver>();
    private readonly ISolutionProjectResolver _slnxResolver = Substitute.For<ISolutionProjectResolver>();

    public class DiscoverTargetFrameworksAsync_FromSolution : SolutionParserFixture
    {
        [Fact]
        public async Task Should_Throw_When_Cancellation_Is_Requested()
        {
            var sut = CreateSut();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(
                () => sut.DiscoverTargetFrameworksAsync("test.sln", [".*"], [], cts.Token));
        }

        [Fact]
        public async Task Should_Delegate_To_Resolver_And_Return_TFs_For_Included_Projects()
        {
            var sut = CreateSut();
            var included = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");
            var excluded = CreateDescriptor("Tests", "/src/Tests/Tests.csproj");

            _slnResolver
                .GetProjectsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns([included, excluded]);

            _assetReader.GetTargetFrameworks("/src/LibA/LibA.csproj").Returns(["net10.0"]);

            var result = await sut.DiscoverTargetFrameworksAsync("test.sln", [".*"], [".*Tests.*"], CancellationToken.None);

            result.ShouldBe(["net10.0"]);
        }
    }

    public class DiscoverTargetFrameworksAsync_FromProjects : SolutionParserFixture
    {
        [Fact]
        public async Task Should_Throw_When_Cancellation_Is_Requested()
        {
            var sut = CreateSut();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(
                () => sut.DiscoverTargetFrameworksAsync(Array.Empty<SolutionProjectDescriptor>(), cts.Token));
        }

        [Fact]
        public async Task Should_Return_Empty_When_No_Projects_Provided()
        {
            var sut = CreateSut();

            var result = await sut.DiscoverTargetFrameworksAsync([], CancellationToken.None);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Strip_Platform_Suffix_And_Deduplicate()
        {
            var sut = CreateSut();
            var project = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");

            _assetReader
                .GetTargetFrameworks("/src/LibA/LibA.csproj")
                .Returns(["net10.0", "net10.0-windows10.0.19041"]);

            var result = await sut.DiscoverTargetFrameworksAsync([project], CancellationToken.None);

            result.Length.ShouldBe(1);
            result[0].ShouldBe("net10.0");
        }

        [Fact]
        public async Task Should_Sort_By_Version_Ascending()
        {
            var sut = CreateSut();
            var projectA = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");
            var projectB = CreateDescriptor("LibB", "/src/LibB/LibB.csproj");

            _assetReader.GetTargetFrameworks("/src/LibA/LibA.csproj").Returns(["net9.0"]);
            _assetReader.GetTargetFrameworks("/src/LibB/LibB.csproj").Returns(["net8.0", "net10.0"]);

            var result = await sut.DiscoverTargetFrameworksAsync([projectA, projectB], CancellationToken.None);

            result.ShouldBe(["net8.0", "net9.0", "net10.0"]);
        }

        [Fact]
        public async Task Should_Deduplicate_Across_Projects()
        {
            var sut = CreateSut();
            var projectA = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");
            var projectB = CreateDescriptor("LibB", "/src/LibB/LibB.csproj");

            _assetReader.GetTargetFrameworks("/src/LibA/LibA.csproj").Returns(["net10.0"]);
            _assetReader.GetTargetFrameworks("/src/LibB/LibB.csproj").Returns(["net10.0"]);

            var result = await sut.DiscoverTargetFrameworksAsync([projectA, projectB], CancellationToken.None);

            result.Length.ShouldBe(1);
            result[0].ShouldBe("net10.0");
        }
    }

    public class DiscoverProjectsAsync : SolutionParserFixture
    {
        [Fact]
        public async Task Should_Dispatch_To_Sln_Resolver()
        {
            var sut = CreateSut();
            var project = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");

            _slnResolver
                .GetProjectsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns([project]);

            var result = await sut.DiscoverProjectsAsync("test.sln", [".*"], [], CancellationToken.None);

            result.AllProjects.Length.ShouldBe(1);
            result.IncludedProjects.Length.ShouldBe(1);
            result.ExcludedProjects.Length.ShouldBe(0);
            result.ImplicitlyExcludedProjects.Length.ShouldBe(0);

            await _slnResolver.Received(1).GetProjectsAsync(
                Arg.Is<string>(path => path.EndsWith(".sln")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Classify_Excluded_Projects()
        {
            var sut = CreateSut();
            var project = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");

            _slnResolver
                .GetProjectsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns([project]);

            var result = await sut.DiscoverProjectsAsync("test.sln", [".*"], [".*LibA.*"], CancellationToken.None);

            result.IncludedProjects.Length.ShouldBe(0);
            result.ExcludedProjects.Length.ShouldBe(1);
            result.ExcludedProjects[0].ProjectName.ShouldBe("LibA");
        }

        [Fact]
        public async Task Should_Classify_Implicitly_Excluded_Projects()
        {
            var sut = CreateSut();
            var project = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");

            _slnResolver
                .GetProjectsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns([project]);

            var result = await sut.DiscoverProjectsAsync("test.sln", [".*Tests.*"], [], CancellationToken.None);

            result.IncludedProjects.Length.ShouldBe(0);
            result.ImplicitlyExcludedProjects.Length.ShouldBe(1);
            result.ImplicitlyExcludedProjects[0].ProjectName.ShouldBe("LibA");
        }

        [Fact]
        public async Task Should_Dispatch_To_Slnx_Resolver()
        {
            var sut = CreateSut();
            var project = CreateDescriptor("LibA", "/src/LibA/LibA.csproj");

            _slnxResolver
                .GetProjectsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns([project]);

            var result = await sut.DiscoverProjectsAsync("test.slnx", [".*"], [], CancellationToken.None);

            result.IncludedProjects.Length.ShouldBe(1);

            await _slnxResolver.Received(1).GetProjectsAsync(
                Arg.Is<string>(path => path.EndsWith(".slnx")), Arg.Any<CancellationToken>());
        }
    }


    private static SolutionProjectDescriptor CreateDescriptor(string projectName, string absolutePath)
    {
        return new SolutionProjectDescriptor(projectName, absolutePath);
    }

    private SolutionParser CreateSut()
    {
        _slnResolver.Extension.Returns(".sln");
        _slnxResolver.Extension.Returns(".slnx");

        return new SolutionParser(_assetReader, [_slnResolver, _slnxResolver], Substitute.For<ILogger<SolutionParser>>());
    }
}
