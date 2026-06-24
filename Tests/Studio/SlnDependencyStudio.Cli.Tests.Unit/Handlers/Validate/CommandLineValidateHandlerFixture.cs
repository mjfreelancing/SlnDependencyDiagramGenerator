using AllOverIt.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;
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

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var validationInvoker = Substitute.For<IValidationInvoker>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, dependencyGenerator, validationInvoker, logger);

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

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var validationInvoker = Substitute.For<IValidationInvoker>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, dependencyGenerator, validationInvoker, logger);

        var result = await handler.HandleAsync(@"X:\nonexistent\file.sds", CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.CannotLoadConfigFile.Value);
    }

    [Fact]
    public async Task Should_Return_CannotLoadConfigFile_When_Json_Is_Malformed()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new JsonException("Invalid JSON"));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var validationInvoker = Substitute.For<IValidationInvoker>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, dependencyGenerator, validationInvoker, logger);

        var result = await handler.HandleAsync(Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.CannotLoadConfigFile.Value);
    }

    [Fact]
    public async Task Should_Return_ValidateCommandFailed_When_Validation_Fails()
    {
        using var tempFile = CreateTempConfigFile("{}");

        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(tempFile.Path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DependencyProjectDocument()));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();

        dependencyGenerator
            .When(generator => generator.ValidateConfiguration(Arg.Any<DependencyGeneratorConfig>()))
            .Do(_ => throw new ValidationException("Test failure"));

        var validationInvoker = Substitute.For<IValidationInvoker>();
        var logger = Substitute.For<ILogger<CommandLineValidateHandler>>();

        var handler = new CommandLineValidateHandler(serializer, dependencyGenerator, validationInvoker, logger);

        var result = await handler.HandleAsync(tempFile.Path, CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.ValidateCommandFailed.Value);
    }

    private static DependencyProjectDocument CreateValidDocument()
    {
        return new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata { ProjectName = "Test", Description = "" },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions
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

    private sealed class DisposableTempFile : IDisposable
    {
        public string Path { get; }

        public DisposableTempFile(string content)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sds");
            File.WriteAllText(Path, content);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private static DisposableTempFile CreateTempConfigFile(string content)
    {
        return new DisposableTempFile(content);
    }
}
