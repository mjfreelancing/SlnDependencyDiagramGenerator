using Shouldly;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using SlnDependencyDiagramGenerator.Tests.Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Parser.Resolvers;

public class SlnxSolutionProjectResolverFixture
{
    private const string SlnxContent = """
        <Solution>
            <Project Path="src\MsBuildProject.csproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
            <Project Path="lib\AnotherBuild.csproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
            <Project Path="src\WebProject.csproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
        </Solution>
        """;

    [Fact]
    public async Task Should_Resolve_Csproj_Projects()
    {
        using var slnxFile = new DisposableTempFile(".slnx", SlnxContent);

        var resolver = new SlnxSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnxFile.FilePath, CancellationToken.None);

        projects.ShouldContain(project => project.ProjectName == "MsBuildProject");
        projects.ShouldContain(project => project.ProjectName == "AnotherBuild");
        projects.ShouldContain(project => project.ProjectName == "WebProject");
    }

    [Fact]
    public async Task Should_Exclude_Non_Proj_Extensions()
    {
        var content = """
            <Solution>
                <Project Path="src\MsBuildProject.csproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
                <Project Path="docs\README.md" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
                <Project Path="config\settings.json" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
            </Solution>
            """;
        using var slnxFile = new DisposableTempFile(".slnx", content);

        var resolver = new SlnxSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnxFile.FilePath, CancellationToken.None);

        projects.ShouldContain(project => project.ProjectName == "MsBuildProject");
        projects.ShouldNotContain(project => project.ProjectName == "README");
        projects.ShouldNotContain(project => project.ProjectName == "settings");
    }

    [Fact]
    public async Task Should_Return_Projects_With_Absolute_Paths()
    {
        using var slnxFile = new DisposableTempFile(".slnx", SlnxContent);

        var resolver = new SlnxSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnxFile.FilePath, CancellationToken.None);

        foreach (var project in projects)
        {
            Path.IsPathRooted(project.AbsolutePath).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Solution_Projects()
    {
        var content = "<Solution></Solution>";
        using var slnxFile = new DisposableTempFile(".slnx", content);

        var resolver = new SlnxSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnxFile.FilePath, CancellationToken.None);

        projects.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Resolve_Projects_With_Different_Proj_Extensions()
    {
        var content = """
            <Solution>
                <Project Path="src\MyApp.csproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
                <Project Path="src\MyLib.vbproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
                <Project Path="src\MyDb.sqlproj" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
            </Solution>
            """;
        using var slnxFile = new DisposableTempFile(".slnx", content);

        var resolver = new SlnxSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnxFile.FilePath, CancellationToken.None);

        projects.Count.ShouldBe(3);
    }
}
