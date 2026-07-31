using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

public class CommandLineRunHandlerFixture
{
    [Fact]
    public async Task Should_Return_Zero_When_All_Dependencies_Succeed()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_When_PreGenerateCommand_Succeeds()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();

        preGenRunner
            .RunAsync(Arg.Any<PreGenerationConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PreGenerationCommandResult
            {
                Succeeded = true,
                ExitCode = 0
            }));

        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_PreGenFailed_When_PreGenerateCommand_Fails_And_Continue_Disabled()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(
                preGenEnabled: true, continueOnFailure: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();

        preGenRunner
            .RunAsync(Arg.Any<PreGenerationConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 1,
                ErrorMessage = "Command failed"
            }));

        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.PreGenerationCommandFailed.Value);
    }

    [Fact]
    public async Task Should_Return_Zero_When_PreGenerateCommand_Fails_And_Continue_Enabled()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(
                preGenEnabled: true, continueOnFailure: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();

        preGenRunner
            .RunAsync(Arg.Any<PreGenerationConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 1,
                ErrorMessage = "Command failed"
            }));

        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

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
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            @"X:\nonexistent\file.sds", CancellationToken.None);

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
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.CannotLoadConfigFile.Value);
    }

    [Fact]
    public async Task Should_Return_ValidateCommandFailed_When_Validation_Fails()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();

        projectValidator
            .When(validator => validator.Validate(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>()))
            .Do(_ => throw new ValidationException("Test failure"));

        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.RunCommandFailed.Value);
    }

    [Fact]
    public async Task Should_Return_DiagramGeneratorFailed_When_Generator_Throws()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();

        dependencyGenerator
            .CreateDiagramsAsync(Arg.Any<DependencyGeneratorConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DependencyGeneratorException("Generator error"));

        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.DiagramGeneratorFailed.Value);
    }

    [Fact]
    public async Task Should_Return_DiagramToolNotFound_When_Tool_Not_Found()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();

        dependencyGenerator
            .CreateDiagramsAsync(Arg.Any<DependencyGeneratorConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ToolNotFoundException("Required external tools are not available"));

        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.DiagramToolNotFound.Value);
    }

    [Fact]
    public async Task Should_Return_RunCommandFailed_When_Cancelled()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(preGenEnabled: false)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();

        dependencyGenerator
            .CreateDiagramsAsync(Arg.Any<DependencyGeneratorConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.RunCommandFailed.Value);
    }

    [Fact]
    public async Task Should_Return_Zero_When_Restore_Succeeds()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(restoreEnabled: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();

        restoreRunner
            .RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestoreSolutionResult { Succeeded = true, ExitCode = 0 }));

        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
        await restoreRunner.Received(1).RunAsync(Path.GetTempPath(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_DotNetRestoreFailed_When_Restore_Fails()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(restoreEnabled: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();

        restoreRunner
            .RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestoreSolutionResult
            {
                Succeeded = false,
                ExitCode = 1,
                ErrorMessage = "Restore failed"
            }));

        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.DotNetRestoreFailed.Value);
        await dependencyGenerator.DidNotReceiveWithAnyArgs().CreateDiagramsAsync(default!, default);
    }

    [Fact]
    public async Task Should_Return_DotNetRestoreFailed_When_Restore_Enabled_But_No_Solution_Path()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(restoreEnabled: true, solutionPath: string.Empty)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();
        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(StudioCliExitCode.DotNetRestoreFailed.Value);
        await restoreRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
    }

    [Fact]
    public async Task Should_Return_Zero_When_PostGeneration_Succeeds()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(postGenEnabled: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();

        postGenRunner
            .RunAsync(Arg.Any<PostGenerationConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PostGenerationCommandResult { Succeeded = true, ExitCode = 0 }));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
        await postGenRunner.Received(1).RunAsync(Arg.Any<PostGenerationConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Zero_When_PostGeneration_Fails()
    {
        var serializer = Substitute.For<IDependencyProjectSerializer>();

        serializer
            .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateValidDocument(postGenEnabled: true)));

        var dependencyGenerator = Substitute.For<IDependencyGenerator>();
        var preGenRunner = Substitute.For<IPreGenerationCommandRunner>();
        var restoreRunner = Substitute.For<IRestoreSolutionRunner>();
        var postGenRunner = Substitute.For<IPostGenerationCommandRunner>();

        postGenRunner
            .RunAsync(Arg.Any<PostGenerationConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PostGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 6,
                ErrorMessage = "Deploy failed"
            }));

        var projectValidator = Substitute.For<IDependencyProjectValidator>();
        var logger = Substitute.For<ILogger<CommandLineRunHandler>>();

        var handler = new CommandLineRunHandler(serializer, dependencyGenerator, restoreRunner, preGenRunner, postGenRunner, projectValidator, logger);

        var result = await handler.HandleAsync(
            Path.Combine(Path.GetTempPath(), "test.sds"), CancellationToken.None);

        result.ShouldBe(0);
    }

    private static DependencyProjectDocument CreateValidDocument(
        bool preGenEnabled = false,
        bool continueOnFailure = false,
        bool restoreEnabled = false,
        string? solutionPath = null,
        bool postGenEnabled = false)
    {
        return new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata { ProjectName = "Test", Description = "" },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionPath ?? Path.GetTempPath(),
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
            PreGeneration = new PreGenerationConfig
            {
                Enabled = preGenEnabled,
                Command = preGenEnabled ? "dotnet" : string.Empty,
                ContinueOnFailure = continueOnFailure
            },
            RestoreSolution = restoreEnabled,
            PostGeneration = new PostGenerationConfig
            {
                Enabled = postGenEnabled,
                Command = postGenEnabled ? "deploy.cmd" : string.Empty
            }
        };
    }
}


