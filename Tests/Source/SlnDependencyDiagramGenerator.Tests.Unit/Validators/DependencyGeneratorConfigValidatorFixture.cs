using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Shared;
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
            using var tempSolution = new DisposableTempFile(".sln");
            var solutionPath = tempSolution.FilePath;

            var model = new TestConfigBuilder()
                .WithSolutionPath(solutionPath)
                .Build();

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
            result.Errors.Count.ShouldBe(0);
        }

        [Fact]
        public void Should_Return_An_Error_When_Projects_Is_Null()
        {
            var model = new DependencyGeneratorConfig
            {
                Solution = null!,
                Diagram = CreateValidDiagramOptions(),
                Export = CreateValidExportOptions()
            };

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Solution");
        }

        [Fact]
        public void Should_Return_An_Error_When_Diagram_Is_Null()
        {
            var model = new DependencyGeneratorConfig
            {
                Solution = CreateValidProjectOptions(),
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
                Solution = CreateValidProjectOptions(),
                Diagram = CreateValidDiagramOptions(),
                Export = null!
            };

            var validator = new DependencyGeneratorConfigValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Export");
        }
    }

    private static GeneratorSolutionOptions CreateValidProjectOptions()
    {
        return new GeneratorSolutionOptions
        {
            SolutionPath = "test.sln",
            RegexToInclude = [".*\\.csproj"],
            RegexToExclude = [],
            PackagesToExclude = [],
            FrameworksToExclude = [],
            Individual = new GeneratorSolutionOptions.ProjectScope
            {
                Enabled = true,
                IncludeDependencies = true,
                TransitiveDepth = 0
            },
            All = new GeneratorSolutionOptions.ProjectScope
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

}

