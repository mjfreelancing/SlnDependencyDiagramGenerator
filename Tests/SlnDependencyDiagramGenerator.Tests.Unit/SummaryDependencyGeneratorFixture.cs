using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Tests.Unit.Support;
using Shouldly;
using System;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class SummaryDependencyGeneratorFixture
{
    public class CreateContent : SummaryDependencyGeneratorFixture
    {
        [Fact]
        public void Should_Return_A_None_Dependency_Section_When_The_Project_Has_No_Dependencies()
        {
            var solutionProjects = CreateSolutionProjects(
                new SolutionProjectBuilder()
                    .WithName("AppConsole")
                    .WithPath(@"C:\\temp\\AppConsole.csproj")
                    .Build());

            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain("## AppConsole");
            content.ShouldContain("### Dependencies");
            content.ShouldContain("* None");
        }

        [Fact]
        public void Should_Return_Framework_Badges_And_Direct_Package_Dependencies()
        {
            var project = new SolutionProjectBuilder()
                .WithName("AppConsole")
                .WithPath(@"C:\\temp\\AppConsole.csproj")
                .WithTargetFrameworks("net10.0")
                .AddFrameworkReference("Microsoft.AspNetCore.App")
                .AddDirectPackage("Newtonsoft.Json", "13.0.3")
                .Build();

            var solutionProjects = CreateSolutionProjects(project);

            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain("## AppConsole");
            content.ShouldContain(".NET-10.0");
            content.ShouldContain("Microsoft.AspNetCore.App");
            content.ShouldContain("Newtonsoft.Json v13.0.3");
        }

        [Fact]
        public void Should_Return_Cross_Project_Version_Conflicts_With_Requested_Version_Details()
        {
            var libV1 = new SolutionProjectBuilder()
                .WithName("LibV1")
                .WithPath(@"C:\\temp\\LibV1.csproj")
                .AddDirectPackage("Newtonsoft.Json", "12.0.3", CreateRequestedDifferentVersionPackage())
                .Build();

            var libV2 = new SolutionProjectBuilder()
                .WithName("LibV2")
                .WithPath(@"C:\\temp\\LibV2.csproj")
                .AddDirectPackage("Newtonsoft.Json", "13.0.3")
                .Build();

            var solutionProjects = CreateSolutionProjects(libV1, libV2);

            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain("## Cross-Project Version Conflicts");
            content.ShouldContain("### Newtonsoft.Json");
            content.ShouldContain("| Project | Resolved | Conflict Details |");
            content.ShouldContain("| LibV1 | 12.0.3 |");
            content.ShouldContain("| LibV2 | 13.0.3 |");
            content.ShouldContain("Via Newtonsoft.Json v12.0.3 requested Newtonsoft.Json >= 13.0.0, resolved v12.0.3");
        }

        private static IDictionary<string, SolutionProject> CreateSolutionProjects(params SolutionProject[] projects)
        {
            var solutionProjects = new Dictionary<string, SolutionProject>(StringComparer.OrdinalIgnoreCase);

            foreach (var project in projects)
            {
                solutionProjects[project.Name] = project;
            }

            return solutionProjects;
        }

        private static PackageReference CreateRequestedDifferentVersionPackage()
        {
            return new PackageReference(false, 0)
            {
                Name = "Newtonsoft.Json",
                Version = "12.0.3",
                RequestedDifferentVersion = true,
                RequestedVersionRange = ">= 13.0.0",
                TransitiveReferences = []
            };
        }
    }
}