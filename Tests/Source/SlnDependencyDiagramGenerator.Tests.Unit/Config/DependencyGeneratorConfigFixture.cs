using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Generator;
using System.IO;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Config;

public class DependencyGeneratorConfigFixture
{
    private readonly IDependencyGenerator _generator;

    public DependencyGeneratorConfigFixture()
    {
        _generator = CreateGenerator();
    }

    [Fact]
    public void Should_Not_Throw_When_Config_Is_Valid()
    {
        var tempSlnPath = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid():N}.sln");

        try
        {
            File.WriteAllText(tempSlnPath, "Microsoft Visual Studio Solution File, Format Version 12.00\n");

            var config = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = tempSlnPath,
                    RegexToInclude = [".*\\.csproj"]
                },
                Diagram = new GeneratorDiagramOptions
                {
                    GroupName = "Test",
                    GroupNameAlias = "test",
                    FrameworkStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    PackageStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    TransitiveStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    Grouping = new GeneratorDiagramOptions.GroupingOptions
                    {
                        BackgroundStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 1.0 }
                    },
                    Formats = [DiagramFormat.D2]
                },
                Export = new GeneratorExportOptions { RootPath = ".\\output" }
            };

            Should.NotThrow(() => _generator.ValidateConfiguration(config));
        }
        finally
        {
            if (File.Exists(tempSlnPath))
            {
                File.Delete(tempSlnPath);
            }
        }
    }

    [Fact]
    public void Should_Throw_ValidationException_When_SolutionPath_Is_Empty()
    {
        var config = new DependencyGeneratorConfig
        {
            Solution = new GeneratorSolutionOptions
            {
                SolutionPath = string.Empty,
                RegexToInclude = [".*\\.csproj"]
            }
        };

        var exception = Should.Throw<ValidationException>(() => _generator.ValidateConfiguration(config));

        exception.Errors.ShouldContain(error => error.ErrorMessage.Contains("SolutionPath"));
    }

    [Fact]
    public void Should_Throw_ValidationException_When_SolutionPath_Is_Wrong_Extension()
    {
        var config = new DependencyGeneratorConfig
        {
            Solution = new GeneratorSolutionOptions
            {
                SolutionPath = "test.txt",
                RegexToInclude = [".*\\.csproj"]
            }
        };

        var exception = Should.Throw<ValidationException>(() => _generator.ValidateConfiguration(config));

        exception.Errors.ShouldContain(error =>
            error.ErrorMessage.Contains(".sln") || error.ErrorMessage.Contains(".slnx"));
    }

    [Fact]
    public void Should_Throw_ValidationException_When_Diagram_Formats_Is_Empty()
    {
        var tempSlnPath = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid():N}.sln");

        try
        {
            File.WriteAllText(tempSlnPath, "Microsoft Visual Studio Solution File, Format Version 12.00\n");

            var config = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = tempSlnPath,
                    RegexToInclude = [".*\\.csproj"]
                },
                Diagram = new GeneratorDiagramOptions
                {
                    GroupName = "Test",
                    GroupNameAlias = "test",
                    FrameworkStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    PackageStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    TransitiveStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 0.8 },
                    Grouping = new GeneratorDiagramOptions.GroupingOptions
                    {
                        BackgroundStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#FFFFFF", Opacity = 1.0 }
                    },
                    Formats = []
                },
                Export = new GeneratorExportOptions { RootPath = ".\\output" }
            };

            var exception = Should.Throw<ValidationException>(() => _generator.ValidateConfiguration(config));

            exception.Errors.ShouldContain(error =>
                error.ErrorMessage.Contains("formats", System.StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(tempSlnPath))
            {
                File.Delete(tempSlnPath);
            }
        }
    }

    private static IDependencyGenerator CreateGenerator()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSlnDependencyGenerator();

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IDependencyGenerator>();
    }
}

