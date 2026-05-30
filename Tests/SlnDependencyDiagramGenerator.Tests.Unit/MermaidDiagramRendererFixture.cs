using AllOverIt.Logging;
using NSubstitute;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;
using Shouldly;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class MermaidDiagramRendererFixture
{
    public class Render : MermaidDiagramRendererFixture
    {
        [Fact]
        public void Should_Render_A_Grouped_Graph_With_Custom_Direction_And_Styles()
        {
            var renderer = new MermaidDiagramRenderer(CreateGroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateGroupedModel());

            content.ShouldContain("flowchart TB");
            content.ShouldContain("  subgraph test[\"All Projects\"]");
            content.ShouldContain("    direction TB");
            content.ShouldContain("    test_appconsole[\"AppConsole\"]");
            content.ShouldContain("  subgraph newtonsoft-json-group[\"Newtonsoft.Json\"]");
            content.ShouldContain("      newtonsoft-json-group_newtonsoft-json_13-0-3[\"Newtonsoft.Json<br>v13.0.3\"]");
            content.ShouldContain("      newtonsoft-json-group_newtonsoft-json_12-0-3[\"Newtonsoft.Json<br>v12.0.3\"]");
            content.ShouldContain("  microsoft-aspnetcore-app[\"Microsoft.AspNetCore.App\"]");
            content.ShouldContain("  test_appconsole --> microsoft-aspnetcore-app");
            content.ShouldContain("  test_appconsole --> newtonsoft-json-group_newtonsoft-json_13-0-3");
            content.ShouldContain("  newtonsoft-json-group_newtonsoft-json_13-0-3 --> newtonsoft-json-group_newtonsoft-json_12-0-3");
            content.ShouldContain("  style test fill:#DDEEFF,stroke:#DDEEFF,stroke-width:1px,opacity:0.75");
            content.ShouldContain("  style newtonsoft-json-group fill:#DDEEFF,stroke:#DDEEFF,stroke-width:1px,opacity:0.75");
            content.ShouldContain("  style microsoft-aspnetcore-app fill:#102030,opacity:0.8");
            content.ShouldContain("  style newtonsoft-json-group_newtonsoft-json_13-0-3 fill:#405060,opacity:0.8");
            content.ShouldContain("  style newtonsoft-json-group_newtonsoft-json_12-0-3 fill:#708090,opacity:0.8");
        }

        [Fact]
        public void Should_Render_Multiple_Project_And_Package_Groups_With_Shared_Project_References()
        {
            var renderer = new MermaidDiagramRenderer(CreateExpandedGroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateExpandedGroupedModel());

            content.ShouldContain("flowchart LR");
            content.ShouldContain("  subgraph test[\"All Projects\"]");
            content.ShouldContain("    direction LR");
            content.ShouldContain("    test_appconsole[\"AppConsole\"]");
            content.ShouldContain("    test_libcore[\"LibCore\"]");
            content.ShouldContain("  subgraph newtonsoft-json-group[\"Newtonsoft.Json\"]");
            content.ShouldContain("  subgraph serilog-group[\"Serilog\"]");
            content.ShouldContain("      newtonsoft-json-group_newtonsoft-json_13-0-3[\"Newtonsoft.Json<br>v13.0.3\"]");
            content.ShouldContain("      newtonsoft-json-group_newtonsoft-json_12-0-3[\"Newtonsoft.Json<br>v12.0.3\"]");
            content.ShouldContain("      serilog-group_serilog_3-1-1[\"Serilog<br>v3.1.1\"]");
            content.ShouldContain("      serilog-group_serilog_2-12-0[\"Serilog<br>v2.12.0\"]");
            content.ShouldContain("  test_appconsole --> test_libcore");
            content.ShouldContain("  test_appconsole --> newtonsoft-json-group_newtonsoft-json_13-0-3");
            content.ShouldContain("  test_libcore --> serilog-group_serilog_3-1-1");
            content.ShouldContain("  style newtonsoft-json-group fill:#DDEEFF,stroke:#DDEEFF,stroke-width:1px,opacity:0.75");
            content.ShouldContain("  style serilog-group fill:#DDEEFF,stroke:#DDEEFF,stroke-width:1px,opacity:0.75");
            content.ShouldContain("  style newtonsoft-json-group_newtonsoft-json_13-0-3 fill:#405060,opacity:0.8");
            content.ShouldContain("  style serilog-group_serilog_3-1-1 fill:#405060,opacity:0.8");
        }

        [Fact]
        public void Should_Render_An_Ungrouped_Graph_With_Right_To_Left_Direction()
        {
            var renderer = new MermaidDiagramRenderer(CreateUngroupedOptions(), Substitute.For<IColorConsoleLogger>());

            var content = renderer.Render(CreateUngroupedModel());

            content.ShouldContain("flowchart RL");
            content.ShouldNotContain("subgraph test");
            content.ShouldContain("  appconsole[\"AppConsole\"]");
            content.ShouldContain("  microsoft-extensions-logging[\"Microsoft.Extensions.Logging\"]");
            content.ShouldContain("  newtonsoft-json_13-0-3[\"Newtonsoft.Json<br>v13.0.3\"]");
            content.ShouldContain("  newtonsoft-json_12-0-3[\"Newtonsoft.Json<br>v12.0.3\"]");
            content.ShouldContain("  appconsole --> microsoft-extensions-logging");
            content.ShouldContain("  appconsole --> newtonsoft-json_13-0-3");
            content.ShouldContain("  newtonsoft-json_13-0-3 --> newtonsoft-json_12-0-3");
            content.ShouldContain("  style microsoft-extensions-logging fill:#010203,opacity:0.9");
            content.ShouldContain("  style newtonsoft-json_13-0-3 fill:#040506,opacity:0.7");
            content.ShouldContain("  style newtonsoft-json_12-0-3 fill:#070809,opacity:0.5");
        }
    }

    private static GeneratorDiagramOptions CreateGroupedOptions()
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.TB,
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