using Shouldly;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using SlnDependencyDiagramGenerator.Tests.Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Parser.Resolvers;

public class SlnSolutionProjectResolverFixture
{
    static SlnSolutionProjectResolverFixture()
    {
        MsBuildSdkResolver.EnsureInitialized();
    }
    private const string SlnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MsBuildProject"", ""src\MsBuildProject.csproj"", ""{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""SolutionItems"", ""SolutionItems"", ""{B2C3D4E5-F6A7-8901-BCDE-F12345678901}""
EndProject
Project(""{E24C65DC-7377-472B-9ABA-BC803B73C61A}"") = ""WebProject"", ""src\WebProject.csproj"", ""{C3D4E5F6-A7B8-9012-CDEF-123456789012}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""AnotherBuild"", ""lib\AnotherBuild.csproj"", ""{D4E5F6A7-B8C9-0123-DEF1-234567890123}""
EndProject
Global
EndGlobal
";

    [Fact]
    public async Task Should_Resolve_KnownToBeMSBuildFormat_Projects()
    {
        using var slnFile = new DisposableTempFile(".sln", SlnContent);

        var resolver = new SlnSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnFile.FilePath, CancellationToken.None);

        projects.ShouldContain(project => project.ProjectName == "MsBuildProject");
        projects.ShouldContain(project => project.ProjectName == "AnotherBuild");
    }

    [Fact]
    public async Task Should_Resolve_WebProjects()
    {
        using var slnFile = new DisposableTempFile(".sln", SlnContent);

        var resolver = new SlnSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnFile.FilePath, CancellationToken.None);

        projects.ShouldContain(project => project.ProjectName == "WebProject");
    }

    [Fact]
    public async Task Should_Exclude_SolutionFolders()
    {
        using var slnFile = new DisposableTempFile(".sln", SlnContent);

        var resolver = new SlnSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnFile.FilePath, CancellationToken.None);

        projects.ShouldNotContain(project => project.ProjectName == "SolutionItems");
    }

    [Fact]
    public async Task Should_Return_Projects_With_Absolute_Paths()
    {
        using var slnFile = new DisposableTempFile(".sln", SlnContent);

        var resolver = new SlnSolutionProjectResolver();
        var projects = await resolver.GetProjectsAsync(slnFile.FilePath, CancellationToken.None);

        foreach (var project in projects)
        {
            Path.IsPathRooted(project.AbsolutePath).ShouldBeTrue();
        }
    }

}
