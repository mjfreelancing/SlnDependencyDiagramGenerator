using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.Services;
using System;
using System.IO;

namespace SlnDependencyStudio.Shared.Tests.Unit.Services;

public class DependencyProjectValidatorFixture
{
    [Fact]
    public void Should_Pass_When_All_Sections_Are_Valid()
    {
        using var solutionFile = new DisposableTempFile(".sln");

        var validator = CreateValidator();
        var document = CreateValidDocument(solutionFile.FilePath);

        Should.NotThrow(() => validator.Validate(document, Environment.CurrentDirectory));
    }

    [Fact]
    public void Should_Throw_When_PreGeneration_Is_Invalid()
    {
        using var solutionFile = new DisposableTempFile(".sln");

        var validator = CreateValidator();
        var document = CreateValidDocument(solutionFile.FilePath);

        document.PreGeneration.Enabled = true;
        document.PreGeneration.Command = string.Empty;

        var exception = Should.Throw<ValidationException>(() => validator.Validate(document, Environment.CurrentDirectory));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("Command", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Throw_When_PostGeneration_Is_Invalid()
    {
        using var solutionFile = new DisposableTempFile(".sln");

        var validator = CreateValidator();
        var document = CreateValidDocument(solutionFile.FilePath);

        document.PostGeneration.Enabled = true;
        document.PostGeneration.Command = string.Empty;

        var exception = Should.Throw<ValidationException>(() => validator.Validate(document, Environment.CurrentDirectory));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("Command", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Throw_When_DiagramGenerator_Is_Invalid()
    {
        var validator = CreateValidator();
        var document = CreateValidDocument(Path.Combine(Path.GetTempPath(), "does-not-exist.sln"));

        var exception = Should.Throw<ValidationException>(() => validator.Validate(document, Environment.CurrentDirectory));

        exception.Errors.ShouldContain(error =>
            error.PropertyName.Contains("SolutionPath", System.StringComparison.OrdinalIgnoreCase));
    }

    private static IDependencyProjectValidator CreateValidator()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IDependencyProjectValidator>();
    }

    private static DependencyProjectDocument CreateValidDocument(string solutionPath)
    {
        return new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata { ProjectName = "Test", Description = "" },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = [".*\\.csproj"],
                    RegexToExclude = [],
                    PackagesToExclude = [],
                    FrameworksToExclude = [],
                    Individual = new GeneratorSolutionOptions.ProjectScope { Enabled = true, TransitiveDepth = 0 },
                    All = new GeneratorSolutionOptions.ProjectScope { Enabled = false, TransitiveDepth = 0 }
                },
                Diagram = new GeneratorDiagramOptions
                {
                    GroupName = "Test",
                    GroupNameAlias = "test",
                    FrameworkStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#000", Opacity = 0.5 },
                    PackageStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#000", Opacity = 0.5 },
                    TransitiveStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#000", Opacity = 0.5 },
                    Grouping = new GeneratorDiagramOptions.GroupingOptions
                    {
                        BackgroundStyle = new GeneratorDiagramOptions.FillStyle { Fill = "#000", Opacity = 1.0 }
                    },
                    Formats = [DiagramFormat.D2]
                },
                Export = new GeneratorExportOptions { RootPath = ".", ImageFormats = [] }
            },
            PreGeneration = new PreGenerationConfig { Enabled = false },
            PostGeneration = new PostGenerationConfig { Enabled = false }
        };
    }

}
