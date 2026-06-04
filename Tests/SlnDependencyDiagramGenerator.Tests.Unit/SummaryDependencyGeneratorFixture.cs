using SlnDependencyDiagramGenerator.Exceptions;
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

        [Fact]
        public void Should_Return_Direct_And_Transitive_Packages()
        {
            var project = new SolutionProjectBuilder()
                .WithName("AppConsole")
                .WithPath(@"C:\\temp\\AppConsole.csproj")
                .AddDirectPackage("Serilog", "3.1.1",
                    SolutionProjectBuilder.CreateTransitivePackage("Newtonsoft.Json", "13.0.3", 1))
                .Build();

            var solutionProjects = CreateSolutionProjects(project);
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain("## AppConsole");
            content.ShouldContain("Serilog v3.1.1");
            content.ShouldContain("Newtonsoft.Json v13.0.3");
        }

        [Fact]
        public void Should_Include_Project_References_In_Dependency_List()
        {
            var libA = new SolutionProjectBuilder()
                .WithName("LibA")
                .WithPath(@"C:\\temp\\LibA.csproj")
                .Build();

            var app = new SolutionProjectBuilder()
                .WithName("AppConsole")
                .WithPath(@"C:\\temp\\AppConsole.csproj")
                .AddProjectReference(@"C:\\temp\\LibA.csproj")
                .Build();

            var solutionProjects = CreateSolutionProjects(libA, app);
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain("## AppConsole");
            content.ShouldContain("### Dependencies");
            content.ShouldContain("* LibA");
        }

        [Fact]
        public void Should_Not_Show_Conflict_Table_When_Same_Package_Has_Same_Version_Across_Projects()
        {
            var libA = new SolutionProjectBuilder()
                .WithName("LibA")
                .WithPath(@"C:\\temp\\LibA.csproj")
                .AddDirectPackage("Newtonsoft.Json", "13.0.3")
                .Build();

            var libB = new SolutionProjectBuilder()
                .WithName("LibB")
                .WithPath(@"C:\\temp\\LibB.csproj")
                .AddDirectPackage("Newtonsoft.Json", "13.0.3")
                .Build();

            var solutionProjects = CreateSolutionProjects(libA, libB);
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldNotContain("Cross-Project Version Conflicts");
            content.ShouldContain("Newtonsoft.Json v13.0.3");
        }

        [Fact]
        public void Should_Show_Conflict_Table_With_Transitive_Chain_Details()
        {
            var transitiveDep = SolutionProjectBuilder.CreateTransitivePackage(
                "Newtonsoft.Json", "12.0.3", 1,
                requestedVersionRange: ">= 13.0.0",
                requestedDifferentVersion: true);

            var libV1 = new SolutionProjectBuilder()
                .WithName("LibV1")
                .WithPath(@"C:\\temp\\LibV1.csproj")
                .AddDirectPackage("Newtonsoft.Json", "12.0.3", transitiveDep)
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
            content.ShouldContain("Via Newtonsoft.Json v12.0.3 requested Newtonsoft.Json >= 13.0.0, resolved v12.0.3");
        }

        [Fact]
        public void Should_Order_Projects_Alphabetically()
        {
            var projects = new[]
            {
                new SolutionProjectBuilder().WithName("ZedApp").WithPath(@"C:\\temp\\ZedApp.csproj").Build(),
                new SolutionProjectBuilder().WithName("AlphaLib").WithPath(@"C:\\temp\\AlphaLib.csproj").Build(),
                new SolutionProjectBuilder().WithName("BetaLib").WithPath(@"C:\\temp\\BetaLib.csproj").Build()
            };

            var solutionProjects = CreateSolutionProjects(projects);
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            var alphaIndex = content.IndexOf("## AlphaLib");
            var betaIndex = content.IndexOf("## BetaLib");
            var zedIndex = content.IndexOf("## ZedApp");

            alphaIndex.ShouldBeLessThan(betaIndex);
            betaIndex.ShouldBeLessThan(zedIndex);
        }

        [Fact]
        public void Should_Include_Multiple_Target_Framework_Badges()
        {
            var project = new SolutionProjectBuilder()
                .WithName("AppConsole")
                .WithPath(@"C:\\temp\\AppConsole.csproj")
                .WithTargetFrameworks("net8.0", "net9.0", "net10.0")
                .Build();

            var solutionProjects = CreateSolutionProjects(project);
            var content = SummaryDependencyGenerator.CreateContent(solutionProjects);

            content.ShouldContain(".NET-8.0");
            content.ShouldContain(".NET-9.0");
            content.ShouldContain(".NET-10.0");
        }

        [Fact]
        public void Should_Throw_When_Project_Reference_Is_Missing_From_Solution()
        {
            var app = new SolutionProjectBuilder()
                .WithName("AppConsole")
                .WithPath(@"C:\\temp\\AppConsole.csproj")
                .AddProjectReference(@"C:\\temp\\MissingLib.csproj")
                .Build();

            var solutionProjects = CreateSolutionProjects(app);

            var exception = Should.Throw<DependencyGeneratorException>(() =>
                SummaryDependencyGenerator.CreateContent(solutionProjects));

            exception.Message.ShouldContain("MissingLib");
        }

        [Fact]
        public void Should_Throw_When_Circular_Project_Reference_Is_Detected()
        {
            var libA = new SolutionProjectBuilder()
                .WithName("LibA")
                .WithPath(@"C:\\temp\\LibA.csproj")
                .AddProjectReference(@"C:\\temp\\LibB.csproj")
                .Build();

            var libB = new SolutionProjectBuilder()
                .WithName("LibB")
                .WithPath(@"C:\\temp\\LibB.csproj")
                .AddProjectReference(@"C:\\temp\\LibA.csproj")
                .Build();

            var solutionProjects = CreateSolutionProjects(libA, libB);

            var exception = Should.Throw<DependencyGeneratorException>(() =>
                SummaryDependencyGenerator.CreateContent(solutionProjects));

            exception.Message.ShouldContain("circular");
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