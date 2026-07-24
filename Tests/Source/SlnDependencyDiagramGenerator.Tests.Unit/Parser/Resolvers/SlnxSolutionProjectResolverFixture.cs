using Shouldly;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
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

    private static string CreateTempSlnxFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.slnx");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task Should_Resolve_Csproj_Projects()
    {
        var slnxFile = CreateTempSlnxFile(SlnxContent);
        try
        {
            var resolver = new SlnxSolutionProjectResolver();
            var projects = await resolver.GetProjectsAsync(slnxFile, CancellationToken.None);

            projects.ShouldContain(project => project.ProjectName == "MsBuildProject");
            projects.ShouldContain(project => project.ProjectName == "AnotherBuild");
            projects.ShouldContain(project => project.ProjectName == "WebProject");
        }
        finally
        {
            File.Delete(slnxFile);
        }
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
        var slnxFile = CreateTempSlnxFile(content);
        try
        {
            var resolver = new SlnxSolutionProjectResolver();
            var projects = await resolver.GetProjectsAsync(slnxFile, CancellationToken.None);

            projects.ShouldContain(project => project.ProjectName == "MsBuildProject");
            projects.ShouldNotContain(project => project.ProjectName == "README");
            projects.ShouldNotContain(project => project.ProjectName == "settings");
        }
        finally
        {
            File.Delete(slnxFile);
        }
    }

    [Fact]
    public async Task Should_Return_Projects_With_Absolute_Paths()
    {
        var slnxFile = CreateTempSlnxFile(SlnxContent);
        try
        {
            var resolver = new SlnxSolutionProjectResolver();
            var projects = await resolver.GetProjectsAsync(slnxFile, CancellationToken.None);

            foreach (var project in projects)
            {
                Path.IsPathRooted(project.AbsolutePath).ShouldBeTrue();
            }
        }
        finally
        {
            File.Delete(slnxFile);
        }
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Solution_Projects()
    {
        var content = "<Solution></Solution>";
        var slnxFile = CreateTempSlnxFile(content);
        try
        {
            var resolver = new SlnxSolutionProjectResolver();
            var projects = await resolver.GetProjectsAsync(slnxFile, CancellationToken.None);

            projects.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(slnxFile);
        }
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
        var slnxFile = CreateTempSlnxFile(content);
        try
        {
            var resolver = new SlnxSolutionProjectResolver();
            var projects = await resolver.GetProjectsAsync(slnxFile, CancellationToken.None);

            projects.Count.ShouldBe(3);
        }
        finally
        {
            File.Delete(slnxFile);
        }
    }
}
