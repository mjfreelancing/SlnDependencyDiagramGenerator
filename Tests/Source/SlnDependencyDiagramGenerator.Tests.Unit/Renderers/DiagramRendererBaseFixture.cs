using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Renderers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Renderers;

public class DiagramRendererBaseFixture
{
    /// <summary>Concrete subclass that exposes protected base members for testing.</summary>
    private sealed class TestDiagramRenderer : DiagramRendererBase
    {
        public override string FileExtension => ".test";

        public TestDiagramRenderer(GeneratorDiagramOptions options, IToolPathResolver toolPathResolver, ILogger logger)
            : base(options, toolPathResolver, logger)
        {
        }

        public override string Render(DependencyGraphModel model) => "rendered";

        protected override string GetDirection() => "TB";

        // Expose protected instance methods
        public new DiagramIntermediateRepresentation BuildIntermediateRepresentation(DependencyGraphModel model)
            => base.BuildIntermediateRepresentation(model);

        public new string ProjectAlias(string projectName)
            => base.ProjectAlias(projectName);

        // Expose protected static methods via public static wrappers
        public static string SanitisePublic(string name) => Sanitise(name);
        public static string PackageAliasPublic(PackageNode pkg, DependencyGraphModel model, bool groupingEnabled)
            => PackageAlias(pkg, model, groupingEnabled);
        public static string FormatElapsedPublic(TimeSpan elapsed) => FormatElapsed(elapsed);
        public static void AssertImageExportSucceededPublic(int exitCode, string toolName, string diagramFileName, string imageFileName)
            => AssertImageExportSucceeded(exitCode, toolName, diagramFileName, imageFileName);

        protected override Task ExportImageFileAsync(string diagramFileName, DiagramImageFormat format, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    // ─── BuildIntermediateRepresentation ────────────────────────────────────

    public class BuildIntermediateRepresentation : DiagramRendererBaseFixture
    {
        [Fact]
        public void Should_Emit_Single_Project_Node()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode { Name = "MyApp", FrameworkReferences = [], PackageReferences = [], ProjectReferences = [] });

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Nodes.Count.ShouldBe(1);
            ir.Nodes.ShouldContain(node => node.Label == "MyApp");
        }

        [Fact]
        public void Should_Emit_Framework_Reference_As_Node_And_Edge()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [new FrameworkNode { Name = "net10.0" }],
                    PackageReferences = [],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Nodes.ShouldContain(node => node.Label == "net10.0");
            ir.Edges.ShouldContain(edge => edge.ToAlias == "net10-0");
        }

        [Fact]
        public void Should_Apply_Framework_Style_Role()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [new FrameworkNode { Name = "net10.0" }],
                    PackageReferences = [],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            var frameworkAlias = "net10-0";
            ir.Styles.ShouldContain(style => style.Alias == frameworkAlias && style.Role == DiagramIrStyleRole.Framework);
        }

        [Fact]
        public void Should_Emit_Package_Reference_As_Node_And_Edge()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [],
                    PackageReferences =
                    [
                        new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false }
                    ],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Nodes.ShouldContain(node => node.Label == "Newtonsoft.Json");
            ir.Edges.ShouldContain(edge => edge.FromAlias == "myapp");
        }

        [Fact]
        public void Should_Apply_Explicit_Package_Style_Role()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [],
                    PackageReferences =
                    [
                        new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false }
                    ],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            var pkgAlias = "newtonsoft-json_13-0-3";
            ir.Styles.ShouldContain(style => style.Alias == pkgAlias && style.Role == DiagramIrStyleRole.PackageExplicit);
        }

        [Fact]
        public void Should_Apply_Transitive_Package_Style_Role()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [],
                    PackageReferences =
                    [
                        new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = true }
                    ],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            var pkgAlias = "newtonsoft-json_13-0-3";
            ir.Styles.ShouldContain(style => style.Alias == pkgAlias && style.Role == DiagramIrStyleRole.PackageTransitive);
        }

        [Fact]
        public void Should_Emit_Project_Reference_As_Node_And_Edge()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [],
                    PackageReferences = [],
                    ProjectReferences = ["LibA.csproj"]
                },
                new ProjectNode
                {
                    Name = "LibA",
                    FrameworkReferences = [],
                    PackageReferences = [],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Nodes.ShouldContain(node => node.Label == "LibA");
            ir.Edges.ShouldContain(edge => edge.FromAlias == "myapp" && edge.ToAlias == "liba");
        }

        [Fact]
        public void Should_Emit_Referenced_Project_Packages()
        {
            var sut = CreateSut();
            var model = CreateModel(
                new ProjectNode
                {
                    Name = "MyApp",
                    FrameworkReferences = [],
                    PackageReferences = [],
                    ProjectReferences = ["LibA.csproj"]
                },
                new ProjectNode
                {
                    Name = "LibA",
                    FrameworkReferences = [],
                    PackageReferences =
                    [
                        new PackageNode { Name = "Serilog", Version = "3.1.0", IsTransitive = false }
                    ],
                    ProjectReferences = []
                });

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Nodes.ShouldContain(node => node.Label == "Serilog");
            ir.Edges.ShouldContain(edge => edge.FromAlias == "liba" && edge.ToAlias.Contains("serilog"));
        }

        [Fact]
        public void Should_Emit_Grouped_Packages_Under_MultiVersion_Group()
        {
            var options = new GeneratorDiagramOptions
            {
                Grouping = new GeneratorDiagramOptions.GroupingOptions { Enabled = true }
            };
            var sut = CreateSut(options);
            var model = CreateModel(
                packagesWithMultipleVersions: new Dictionary<string, string>
                {
                    ["Newtonsoft.Json"] = "newtonsoft-json"
                },
                projects:
                [
                    new ProjectNode
                    {
                        Name = "MyApp",
                        FrameworkReferences = [],
                        PackageReferences =
                        [
                            new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false }
                        ],
                        ProjectReferences = []
                    }
                ]);

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Groups.ShouldContain(group => group.Alias == "newtonsoft-json-group");
        }

        [Fact]
        public void Should_Not_Emit_Group_When_Grouping_Disabled()
        {
            var sut = CreateSut();
            var model = CreateModel(
                packagesWithMultipleVersions: new Dictionary<string, string>
                {
                    ["Newtonsoft.Json"] = "newtonsoft-json"
                },
                projects:
                [
                    new ProjectNode
                    {
                        Name = "MyApp",
                        FrameworkReferences = [],
                        PackageReferences =
                        [
                            new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3", IsTransitive = false }
                        ],
                        ProjectReferences = []
                    }
                ]);

            var ir = sut.BuildIntermediateRepresentation(model);

            ir.Groups.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Throw_When_Circular_Project_Reference_Is_Detected()
        {
            var sut = CreateSut();

            var model = CreateModel(
                new ProjectNode
                {
                    Name = "LibA",
                    FrameworkReferences = [],
                    PackageReferences = [],
                    ProjectReferences = [@"C:\sln\LibB\LibB.csproj"]
                },
                new ProjectNode
                {
                    Name = "LibB",
                    FrameworkReferences = [],
                    PackageReferences = [],
                    ProjectReferences = [@"C:\sln\LibA\LibA.csproj"]
                });

            var exception = Should.Throw<DependencyGraphException>(() => sut.BuildIntermediateRepresentation(model));

            exception.Message.ShouldContain("circular");
        }
    }

    // ─── ProjectAlias ───────────────────────────────────────────────────────

    public class ProjectAliasMethod : DiagramRendererBaseFixture
    {
        [Fact]
        public void Should_Sanitise_And_Return_Without_Group_Prefix_When_Grouping_Disabled()
        {
            var sut = CreateSut();
            var result = sut.ProjectAlias("MyApp.Lib");

            result.ShouldBe("myapp-lib");
        }

        [Fact]
        public void Should_Prefix_With_GroupAlias_When_Grouping_Enabled()
        {
            var options = new GeneratorDiagramOptions
            {
                GroupNameAlias = "mygroup",
                Grouping = new GeneratorDiagramOptions.GroupingOptions { Enabled = true }
            };
            var sut = CreateSut(options);
            var result = sut.ProjectAlias("MyApp.Lib");

            result.ShouldBe("mygroup.myapp-lib");
        }
    }

    // ─── Sanitise ───────────────────────────────────────────────────────────

    public class SanitiseMethod : DiagramRendererBaseFixture
    {
        [Theory]
        [InlineData("MyApp", "myapp")]
        [InlineData("MyApp.Lib", "myapp-lib")]
        [InlineData("Some.Thing.V1", "some-thing-v1")]
        [InlineData("", "")]
        public void Should_Replace_Dots_With_Hyphens_And_Lowercase(string input, string expected)
        {
            var result = TestDiagramRenderer.SanitisePublic(input);
            result.ShouldBe(expected);
        }
    }

    // ─── PackageAlias ───────────────────────────────────────────────────────

    public class PackageAliasMethod : DiagramRendererBaseFixture
    {
        [Fact]
        public void Should_Return_BaseAlias_When_Grouping_Disabled()
        {
            var pkg = new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3" };
            var model = CreateModel();

            var result = TestDiagramRenderer.PackageAliasPublic(pkg, model, groupingEnabled: false);

            result.ShouldBe("newtonsoft-json_13-0-3");
        }

        [Fact]
        public void Should_Return_BaseAlias_When_Grouping_Enabled_But_Not_MultiVersion()
        {
            var pkg = new PackageNode { Name = "Serilog", Version = "3.1.0" };
            var model = CreateModel();

            var result = TestDiagramRenderer.PackageAliasPublic(pkg, model, groupingEnabled: true);

            result.ShouldBe("serilog_3-1-0");
        }

        [Fact]
        public void Should_Nest_Under_Group_When_MultiVersion()
        {
            var pkg = new PackageNode { Name = "Newtonsoft.Json", Version = "13.0.3" };
            var model = CreateModel(
                packagesWithMultipleVersions: new Dictionary<string, string>
                {
                    ["Newtonsoft.Json"] = "newtonsoft-json"
                });

            var result = TestDiagramRenderer.PackageAliasPublic(pkg, model, groupingEnabled: true);

            result.ShouldBe("newtonsoft-json-group.newtonsoft-json_13-0-3");
        }
    }

    // ─── FormatElapsed ──────────────────────────────────────────────────────

    public class FormatElapsedMethod : DiagramRendererBaseFixture
    {
        [Theory]
        [InlineData(0, 0, 0, 0, "0.00s")]
        [InlineData(0, 0, 0, 500, "0.50s")]
        [InlineData(0, 0, 1, 0, "1.00s")]
        [InlineData(0, 0, 1, 500, "1.50s")]
        [InlineData(0, 0, 59, 990, "59.99s")]
        [InlineData(0, 1, 0, 0, "60.00s")]
        [InlineData(1, 0, 0, 0, "3600.00s")]
        public void Should_Format_Elapsed_Time_With_Two_Decimal_Seconds(
            int hours, int minutes, int seconds, int milliseconds, string expected)
        {
            var elapsed = new TimeSpan(0, hours, minutes, seconds, milliseconds);
            var result = TestDiagramRenderer.FormatElapsedPublic(elapsed);
            result.ShouldBe(expected);
        }
    }

    // ─── AssertImageExportSucceeded ─────────────────────────────────────────

    public class AssertImageExportSucceeded : DiagramRendererBaseFixture
    {
        [Fact]
        public void Should_Throw_When_Exit_Code_Is_Non_Zero()
        {
            var imageFileName = Path.Combine(Path.GetTempPath(), $"assert-export-{Guid.NewGuid():N}.png");

            var exception = Should.Throw<DiagramImageExportException>(() =>
                TestDiagramRenderer.AssertImageExportSucceededPublic(1, "d2", "test.d2", imageFileName));

            exception.Message.ShouldContain("'d2' failed to export an image for 'test.d2' (exit code 1)");
        }

        [Fact]
        public void Should_Throw_When_Output_File_Not_Created()
        {
            var imageFileName = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");
            File.Delete(imageFileName);

            var exception = Should.Throw<DiagramImageExportException>(() =>
                TestDiagramRenderer.AssertImageExportSucceededPublic(0, "mmdc", "test.mmd", imageFileName));

            exception.Message.ShouldContain("did not create the expected image file");
        }

        [Fact]
        public void Should_Not_Throw_When_Exit_Code_Zero_And_File_Exists()
        {
            var imageFileName = Path.Combine(Path.GetTempPath(), $"existing-{Guid.NewGuid():N}.png");

            try
            {
                File.WriteAllText(imageFileName, "content");

                Should.NotThrow(() =>
                    TestDiagramRenderer.AssertImageExportSucceededPublic(0, "d2", "test.d2", imageFileName));
            }
            finally
            {
                File.Delete(imageFileName);
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static GeneratorDiagramOptions DefaultOptions() => new()
    {
        Grouping = new GeneratorDiagramOptions.GroupingOptions { Enabled = false }
    };

    private static TestDiagramRenderer CreateSut(GeneratorDiagramOptions? options = null)
    {
        return new TestDiagramRenderer(
            options ?? DefaultOptions(),
            Substitute.For<IToolPathResolver>(),
            Substitute.For<ILogger>());
    }

    private static DependencyGraphModel CreateModel(
        ProjectNode[]? projects = null,
        Dictionary<string, string>? packagesWithMultipleVersions = null)
    {
        return new DependencyGraphModel
        {
            Projects = projects ?? [],
            PackagesWithMultipleVersions = packagesWithMultipleVersions ?? new Dictionary<string, string>()
        };
    }

    private static DependencyGraphModel CreateModel(params ProjectNode[] projects)
    {
        return CreateModel(projects, null);
    }
}
