using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Unit.Support;
using SlnDependencyDiagramGenerator.Validators;
using Shouldly;
using System;
using System.IO;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Validators;

public class DependencyGeneratorConfigValidatorFixture
{
    public class Validate : DependencyGeneratorConfigValidatorFixture
    {
        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_Model()
        {
            var solutionPath = CreateTempFilePath(".sln");

            try
            {
                var model = new TestConfigBuilder()
                    .WithSolutionPath(solutionPath)
                    .Build();

                var validator = new DependencyGeneratorConfigValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeTrue();
                result.Errors.Count.ShouldBe(0);
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Projects_Is_Null()
        {
            var model = new DependencyGeneratorConfig
            {
                Projects = null!,
                Diagram = CreateValidDiagramOptions(),
                Export = CreateValidExportOptions()
            };

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Projects");
        }

        [Fact]
        public void Should_Return_An_Error_When_Diagram_Is_Null()
        {
            var model = new DependencyGeneratorConfig
            {
                Projects = CreateValidProjectOptions(),
                Diagram = null!,
                Export = CreateValidExportOptions()
            };

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Diagram");
        }

        [Fact]
        public void Should_Return_An_Error_When_Export_Is_Null()
        {
            var model = new DependencyGeneratorConfig
            {
                Projects = CreateValidProjectOptions(),
                Diagram = CreateValidDiagramOptions(),
                Export = null!
            };

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Export");
        }
    }

    private static GeneratorProjectOptions CreateValidProjectOptions()
    {
        return new GeneratorProjectOptions
        {
            SolutionPath = "test.sln",
            RegexToInclude = [".*\\.csproj"],
            RegexToExclude = [],
            PackagesToExclude = [],
            FrameworksToExclude = [],
            Individual = new GeneratorProjectOptions.ProjectScope
            {
                Enabled = true,
                IncludeDependencies = true,
                TransitiveDepth = 0
            },
            All = new GeneratorProjectOptions.ProjectScope
            {
                Enabled = true,
                IncludeDependencies = true,
                TransitiveDepth = 0
            }
        };
    }

    private static GeneratorDiagramOptions CreateValidDiagramOptions()
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.LR,
            GroupName = "Group",
            GroupNameAlias = "group",
            FrameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            PackageStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            TransitiveStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            Grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                }
            },
            Formats = [DiagramFormat.D2]
        };
    }

    private static GeneratorExportOptions CreateValidExportOptions()
    {
        return new GeneratorExportOptions
        {
            RootPath = "output",
            ImageFormats = [DiagramImageFormat.Png]
        };
    }

    private static string CreateTempFilePath(string extension)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        File.WriteAllText(filePath, string.Empty);

        return filePath;
    }
}