using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Exceptions;
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
    public async Task Should_Return_ConfigFileNotFound_When_File_Does_Not_Exist()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FileNotFoundException("File not found"));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(@"X:\nonexistent\file.sds", CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadConfigFile);
    }

    [Fact]
    public async Task Should_Return_CannotLoadConfigFile_When_Json_Is_Malformed()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new JsonException("Invalid JSON"));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadConfigFile);
    }

    [Fact]
    public async Task Should_Return_CannotLoadConfigFile_When_Project_Document_Is_Invalid()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependencyProjectException("The document is empty or not a valid dependency project document"));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, projectValidator, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe((int)StudioCliExitCode.CannotLoadConfigFile);
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

}

