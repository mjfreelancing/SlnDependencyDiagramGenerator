using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Exceptions;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Services;
using System.Text.Json;

namespace SlnDependencyStudio.Cli.Tests.Unit.Handlers.Validate;

public class CommandLineValidateHandlerFixture
{
    [Fact]
    public async Task Should_Return_Zero_When_All_Dependencies_Succeed()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument()));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_ProjectFileNotFound_When_File_Does_Not_Exist()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("File not found"));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(@"X:\nonexistent\file.sds", CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadProjectFile);
    }

    [Fact]
    public async Task Should_Return_CannotLoadProjectFile_When_Json_Is_Malformed()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new JsonException("Invalid JSON"));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadProjectFile);
    }

    [Fact]
    public async Task Should_Return_CannotLoadProjectFile_When_Project_File_Is_Invalid()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependencyProjectException("The dependency project content is empty or invalid."));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadProjectFile);
    }

    [Fact]
    public async Task Should_Return_ValidateCommandFailed_When_Validation_Fails()
    {
        using var tempFile = new DisposableTempFile(".sds", "{}");

        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(tempFile.FilePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DependencyProjectDocument()));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();

        projectValidator
            .When(validator => validator.Validate(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>()))
            .Do(_ => throw new ValidationException("Test failure"));

        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(tempFile.FilePath, CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.ValidateCommandFailed);
    }

    [Fact]
    public async Task Should_Return_ValidateCommandFailed_When_Regex_Is_Invalid()
    {
        using var projectFile = new DisposableTempFile(".sds", "{}");
        using var solutionFile = new DisposableTempFile(".sln");

        var document = new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata { ProjectName = "Test", Description = "" },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionFile.FilePath,
                    RegexToInclude = ["("],
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

        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(projectFile.FilePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(document));

        // Uses the real validation chain: the invalid include pattern is turned into a ValidationException
        // (GeneratorSolutionOptionsValidator.ValidateRegexPattern swallows the RegexParseException and reports
        // a rule failure), which the handler maps to ValidateCommandFailed - the regex error never escapes the
        // handler as an unexpected failure.
        var projectValidator = CreateValidator();

        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(projectFile.FilePath, CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.ValidateCommandFailed);
    }

    [Fact]
    public async Task Should_Rethrow_OperationCanceledException_When_Cancelled()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        // The handler logs the cancellation and rethrows; App owns the exit-code mapping so the code is
        // not coupled to whichever handler happened to observe the cancellation.
        await Should.ThrowAsync<OperationCanceledException>(() => handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None));
    }

    private static DependencyProjectDocument CreateValidDocument()
    {
        return new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata { ProjectName = "Test", Description = "" },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = Path.GetTempPath(),
                    RegexToInclude = [".*\\.csproj"]
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
                Export = new GeneratorExportOptions { RootPath = "." }
            },
            PreGeneration = new PreGenerationConfig { Enabled = false }
        };
    }

    private static IDependencyProjectValidator CreateValidator()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyDiagramGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IDependencyProjectValidator>();
    }

}

