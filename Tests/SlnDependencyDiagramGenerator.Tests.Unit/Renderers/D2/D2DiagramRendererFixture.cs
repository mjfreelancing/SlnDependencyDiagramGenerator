using AllOverIt.Logging;
using NSubstitute;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using SlnDependencyDiagramGenerator.Renderers.D2;
using Shouldly;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Renderers.D2;

public class D2DiagramRendererFixture
{
    public class Render : D2DiagramRendererFixture
    {
        [Fact]
        public void Should_Render_A_Grouped_Graph_With_Custom_Direction_And_Styles()
        {
            var renderer = new D2DiagramRenderer(CreateGroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateGroupedModel());

            content.ShouldContain("direction: up");
            content.ShouldContain("test: All Projects");
            content.ShouldContain("newtonsoft-json-group: \"\"");
            content.ShouldContain("test.appconsole: AppConsole");
            content.ShouldContain("microsoft-aspnetcore-app: Microsoft.AspNetCore.App");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_13-0-3: Newtonsoft.Json");
            content.ShouldContain("v13.0.3");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_12-0-3: Newtonsoft.Json");
            content.ShouldContain("v12.0.3");
            content.ShouldContain("microsoft-aspnetcore-app.style.fill: \"#102030\"");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_13-0-3.style.fill: \"#405060\"");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_12-0-3.style.fill: \"#708090\"");
            content.ShouldContain("test.style.fill: \"#DDEEFF\"");
            content.ShouldContain("newtonsoft-json-group.style.fill: \"#DDEEFF\"");
        }

        [Fact]
        public void Should_Render_Multiple_Project_And_Package_Groups_With_Shared_Project_References()
        {
            var renderer = new D2DiagramRenderer(CreateExpandedGroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateExpandedGroupedModel());

            content.ShouldContain("direction: left");
            content.ShouldContain("test.appconsole: AppConsole");
            content.ShouldContain("test.libcore: LibCore");
            content.ShouldContain("newtonsoft-json-group: \"\"");
            content.ShouldContain("serilog-group: \"\"");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_13-0-3: Newtonsoft.Json");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_12-0-3: Newtonsoft.Json");
            content.ShouldContain("serilog-group.serilog_3-1-1: Serilog");
            content.ShouldContain("serilog-group.serilog_2-12-0: Serilog");
            content.ShouldContain("test.libcore <- test.appconsole");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_13-0-3 <- test.appconsole");
            content.ShouldContain("serilog-group.serilog_3-1-1 <- test.libcore");
            content.ShouldContain("newtonsoft-json-group.style.fill: \"#DDEEFF\"");
            content.ShouldContain("serilog-group.style.fill: \"#DDEEFF\"");
            content.ShouldContain("newtonsoft-json-group.newtonsoft-json_13-0-3.style.fill: \"#405060\"");
            content.ShouldContain("serilog-group.serilog_3-1-1.style.fill: \"#405060\"");
        }

        [Fact]
        public void Should_Render_An_Ungrouped_Graph_With_Right_To_Left_Direction()
        {
            var renderer = new D2DiagramRenderer(CreateUngroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateUngroupedModel());

            content.ShouldContain("direction: right");
            content.ShouldNotContain("test:");
            content.ShouldNotContain("subgraph");
            content.ShouldContain("appconsole: AppConsole");
            content.ShouldContain("microsoft-extensions-logging: Microsoft.Extensions.Logging");
            content.ShouldContain("newtonsoft-json_13-0-3: Newtonsoft.Json");
            content.ShouldContain("v13.0.3");
            content.ShouldContain("newtonsoft-json_12-0-3: Newtonsoft.Json");
            content.ShouldContain("v12.0.3");
            content.ShouldContain("microsoft-extensions-logging.style.fill: \"#010203\"");
            content.ShouldContain("newtonsoft-json_13-0-3.style.fill: \"#040506\"");
            content.ShouldContain("newtonsoft-json_12-0-3.style.fill: \"#070809\"");
        }
    }

    private static GeneratorDiagramOptions CreateGroupedOptions()
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.BT,
            GroupName = "All Projects",
            GroupNameAlias = "test",
            FrameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#102030",
                Opacity = 0.8
            },
            PackageStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#405060",
                Opacity = 0.8
            },
            TransitiveStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#708090",
                Opacity = 0.8
            },
            Grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                Enabled = true,
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#DDEEFF",
                    Opacity = 0.75
                }
            }
        };
    }

    private static GeneratorDiagramOptions CreateExpandedGroupedOptions()
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.LR,
            GroupName = "All Projects",
            GroupNameAlias = "test",
            FrameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#102030",
                Opacity = 0.8
            },
            PackageStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#405060",
                Opacity = 0.8
            },
            TransitiveStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#708090",
                Opacity = 0.8
            },
            Grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                Enabled = true,
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#DDEEFF",
                    Opacity = 0.75
                }
            }
        };
    }

    private static GeneratorDiagramOptions CreateUngroupedOptions()
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.RL,
            GroupName = "All Projects",
            GroupNameAlias = "test",
            FrameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#010203",
                Opacity = 0.9
            },
            PackageStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#040506",
                Opacity = 0.7
            },
            TransitiveStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#070809",
                Opacity = 0.5
            },
            Grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                Enabled = false,
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#DDEEFF",
                    Opacity = 0.75
                }
            }
        };
    }

    private static DependencyGraphModel CreateGroupedModel()
    {
        return new DependencyGraphModel
        {
            Projects =
            [
                new ProjectNode
                {
                    Name = "AppConsole",
                    FrameworkReferences =
                    [
                        new FrameworkNode
                        {
                            Name = "Microsoft.AspNetCore.App"
                        }
                    ],
                    PackageReferences =
                    [
                        new PackageNode
                        {
                            Name = "Newtonsoft.Json",
                            Version = "13.0.3",
                            IsTransitive = false,
                            Depth = 0,
                            TransitiveReferences =
                            [
                                new PackageNode
                                {
                                    Name = "Newtonsoft.Json",
                                    Version = "12.0.3",
                                    IsTransitive = true,
                                    Depth = 1
                                }
                            ]
                        }
                    ]
                }
            ],
            PackagesWithMultipleVersions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Newtonsoft.Json"] = "newtonsoft-json"
            }
        };
    }

    private static DependencyGraphModel CreateExpandedGroupedModel()
    {
        return new DependencyGraphModel
        {
            Projects =
            [
                new ProjectNode
                {
                    Name = "AppConsole",
                    ProjectReferences = ["LibCore.csproj"],
                    PackageReferences =
                    [
                        new PackageNode
                        {
                            Name = "Newtonsoft.Json",
                            Version = "13.0.3",
                            IsTransitive = false,
                            Depth = 0,
                            TransitiveReferences =
                            [
                                new PackageNode
                                {
                                    Name = "Newtonsoft.Json",
                                    Version = "12.0.3",
                                    IsTransitive = true,
                                    Depth = 1
                                }
                            ]
                        }
                    ]
                },
                new ProjectNode
                {
                    Name = "LibCore",
                    PackageReferences =
                    [
                        new PackageNode
                        {
                            Name = "Serilog",
                            Version = "3.1.1",
                            IsTransitive = false,
                            Depth = 0,
                            TransitiveReferences =
                            [
                                new PackageNode
                                {
                                    Name = "Serilog",
                                    Version = "2.12.0",
                                    IsTransitive = true,
                                    Depth = 1
                                }
                            ]
                        }
                    ]
                }
            ],
            PackagesWithMultipleVersions = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Newtonsoft.Json"] = "newtonsoft-json",
                ["Serilog"] = "serilog"
            }
        };
    }

    private static DependencyGraphModel CreateUngroupedModel()
    {
        return new DependencyGraphModel
        {
            Projects =
            [
                new ProjectNode
                {
                    Name = "AppConsole",
                    FrameworkReferences =
                    [
                        new FrameworkNode
                        {
                            Name = "Microsoft.Extensions.Logging"
                        }
                    ],
                    PackageReferences =
                    [
                        new PackageNode
                        {
                            Name = "Newtonsoft.Json",
                            Version = "13.0.3",
                            IsTransitive = false,
                            Depth = 0,
                            TransitiveReferences =
                            [
                                new PackageNode
                                {
                                    Name = "Newtonsoft.Json",
                                    Version = "12.0.3",
                                    IsTransitive = true,
                                    Depth = 1
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}