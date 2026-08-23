using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.ProcessExecution;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Services;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Run;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Run;

[Collection(nameof(ReactiveUIInitializer))]
public class GenerationServiceFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IScopedOperationFactory<IRestoreSolutionRunner> _restoreRunnerFactory = Substitute.For<IScopedOperationFactory<IRestoreSolutionRunner>>();
    private readonly IScopedOperationFactory<IPreGenerationCommandRunner> _runnerFactory = Substitute.For<IScopedOperationFactory<IPreGenerationCommandRunner>>();
    private readonly IScopedOperationFactory<IPostGenerationCommandRunner> _postGenRunnerFactory = Substitute.For<IScopedOperationFactory<IPostGenerationCommandRunner>>();
    private readonly IScopedOperationFactory<IDependencyGenerator> _generatorFactory = Substitute.For<IScopedOperationFactory<IDependencyGenerator>>();
    private readonly IDependencyProjectValidator _projectValidator = Substitute.For<IDependencyProjectValidator>();
    private readonly RecordingLogger<GenerationService> _logger = new();

    private readonly TrackableValue<bool> _preGenEnabled = new();
    private readonly TrackableValue<string> _preGenCommand = new();
    private readonly TrackableValue<string> _preGenArguments = new();
    private readonly TrackableValue<string> _preGenWorkingDirectory = new();
    private readonly TrackableValue<bool> _preGenContinueOnFailure = new();
    private readonly IPreGenerationConfigEditor _preGenEditor = Substitute.For<IPreGenerationConfigEditor>();

    private readonly TrackableValue<bool> _restoreSolution = new();
    private readonly IRestoreSolutionEditor _restoreSolutionEditor = Substitute.For<IRestoreSolutionEditor>();

    private readonly TrackableValue<bool> _postGenEnabled = new();
    private readonly TrackableValue<string> _postGenCommand = new();
    private readonly TrackableValue<string> _postGenArguments = new();
    private readonly TrackableValue<string> _postGenWorkingDirectory = new();
    private readonly IPostGenerationConfigEditor _postGenEditor = Substitute.For<IPostGenerationConfigEditor>();

    private readonly GenerationService _service;

    public GenerationServiceFixture()
    {
        _preGenEnabled.SetOriginalValue(false);
        _preGenCommand.SetOriginalValue(string.Empty);
        _preGenArguments.SetOriginalValue(string.Empty);
        _preGenWorkingDirectory.SetOriginalValue(string.Empty);
        _preGenContinueOnFailure.SetOriginalValue(false);

        _restoreSolution.SetOriginalValue(false);
        _postGenEnabled.SetOriginalValue(false);
        _postGenCommand.SetOriginalValue(string.Empty);
        _postGenArguments.SetOriginalValue(string.Empty);
        _postGenWorkingDirectory.SetOriginalValue(string.Empty);

        _preGenEditor.Enabled.Returns(_preGenEnabled);
        _preGenEditor.Command.Returns(_preGenCommand);
        _preGenEditor.Arguments.Returns(_preGenArguments);
        _preGenEditor.WorkingDirectory.Returns(_preGenWorkingDirectory);
        _preGenEditor.ContinueOnFailure.Returns(_preGenContinueOnFailure);
        _store.PreGenerationEditor.Returns(_preGenEditor);

        _restoreSolutionEditor.RestoreSolution.Returns(_restoreSolution);
        _store.RestoreSolutionEditor.Returns(_restoreSolutionEditor);

        _postGenEditor.Enabled.Returns(_postGenEnabled);
        _postGenEditor.Command.Returns(_postGenCommand);
        _postGenEditor.Arguments.Returns(_postGenArguments);
        _postGenEditor.WorkingDirectory.Returns(_postGenWorkingDirectory);
        _store.PostGenerationEditor.Returns(_postGenEditor);

        _service = new GenerationService(_store, _projectValidator, _restoreRunnerFactory, _runnerFactory, _postGenRunnerFactory, _generatorFactory, _logger);
    }

    public class RunPreGenerationAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Return_True_When_Disabled()
        {
            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeTrue();
            _logger.Records.ShouldHaveSingleItem();
            _logger.Records[0].Level.ShouldBe(LogLevel.Debug);
            _logger.Records[0].Message.ShouldBe("Pre-generation command disabled or has no command — skipping");
        }

        [Fact]
        public async Task Should_Return_True_When_Command_Is_Empty()
        {
            _preGenEnabled.Value = true;

            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeTrue();
            _logger.Records.ShouldHaveSingleItem();
            _logger.Records[0].Level.ShouldBe(LogLevel.Debug);
            _logger.Records[0].Message.ShouldBe("Pre-generation command disabled or has no command — skipping");
        }

        [Fact]
        public async Task Should_Return_True_On_Success()
        {
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ExitCode = 0
            });

            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Return_False_When_ContinueOnFailure_Is_False()
        {
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Return_True_When_ContinueOnFailure_Is_True()
        {
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            _preGenContinueOnFailure.Value = true; SetupPreGenRunner(new PreGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Return_False_When_Cancelled()
        {
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.Cancelled,
                ErrorMessage = "Cancelled"
            });

            var result = await _service.RunPreGenerationAsync(CancellationToken.None);

            result.ShouldBeFalse();

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Warning && record.Message.Contains("cancelled"));
        }

        [Fact]
        public async Task Should_Resolve_Relative_WorkingDirectory()
        {
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";
            _preGenWorkingDirectory.Value = "build";
            _store.DocumentFilePath.Returns(@"C:\Projects\myproject.sds");

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ExitCode = 0
            });

            await _service.RunPreGenerationAsync(CancellationToken.None);

            await _runnerFactory.Received(1).ExecuteAsync(
                Arg.Any<Func<IPreGenerationCommandRunner, CancellationToken, Task<PreGenerationCommandResult>>>(),
                Arg.Any<CancellationToken>());
        }
    }

    public class RunDiagramGenerationAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Log_Generating_Message()
        {
            SetupGenerator();

            var config = new DependencyGeneratorConfig();

            await _service.RunDiagramGenerationAsync(config, CancellationToken.None);

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Information && record.Message == "Generating diagrams...");
        }
    }

    public class RunAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Log_Started_And_Completed_When_Successful()
        {
            SetupStoreConfig();
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Information && record.Message == "Starting generation");

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Information && record.Message.StartsWith("Generation completed ("));
        }

        [Fact]
        public async Task Should_Log_Error_When_Generator_Throws()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new InvalidOperationException("Something went wrong"));

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Error && record.Message == "Generation failed unexpectedly");
        }

        [Fact]
        public async Task Should_Log_Cancelled_When_OperationCanceledException()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new OperationCanceledException());

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Warning && record.Message == "Generation cancelled");
        }

        [Fact]
        public async Task Should_Log_All_Validation_Errors_When_Validation_Fails()
        {
            SetupStoreConfig();

            var failures = new[]
            {
                new ValidationFailure(nameof(PreGenerationConfig.WorkingDirectory), "Pre-generation working directory not found"),
                new ValidationFailure(nameof(PostGenerationConfig.Command), "Post-generation command must not be empty")
            };

            _projectValidator
                .When(validator => validator.Validate(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>()))
                .Do(_ => throw new ValidationException(failures));

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Error && record.Message == "Configuration validation failed:");

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Error && record.Message == "  - Pre-generation working directory not found");

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Error && record.Message == "  - Post-generation command must not be empty");
        }

        [Fact]
        public async Task Should_Run_Generator_After_PreGen_Success()
        {
            SetupStoreConfig();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult { ExitCode = 0 });
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record => record.Message == "Generating diagrams...");
            _logger.Records.ShouldContain(record => record.Message.StartsWith("Generation completed ("));
        }

        [Fact]
        public async Task Should_Abort_When_PreGen_Fails_Without_Continue()
        {
            SetupStoreConfig();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldNotContain(record => record.Message == "Generating diagrams...");

            _ = _generatorFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_Continue_When_PreGen_Fails_With_Continue()
        {
            SetupStoreConfig();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";
            _preGenContinueOnFailure.Value = true;

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record => record.Message == "Pre-generation command failed (continuing): Build failed");
            _logger.Records.ShouldContain(record => record.Message == "Generating diagrams...");
            _logger.Records.ShouldContain(record => record.Message.StartsWith("Generation completed ("));
        }

        [Fact]
        public async Task Should_Build_Config_From_Store()
        {
            SetupStoreConfig();
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _store.Received(1).BuildDocument();
        }

        [Fact]
        public async Task Should_Abort_When_Restore_Fails()
        {
            SetupStoreConfig(@"C:\Projects\test.sln");

            _restoreSolution.Value = true;

            SetupRestoreRunner(new RestoreSolutionResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 4,
                ErrorMessage = "Restore failed"
            });

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldNotContain(record => record.Message == "Generating diagrams...");
            _ = _generatorFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_Run_Generator_After_Restore_Success()
        {
            SetupStoreConfig(@"C:\Projects\test.sln");

            _restoreSolution.Value = true;

            SetupRestoreRunner(new RestoreSolutionResult { ExitCode = 0 });
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record => record.Message == "Generating diagrams...");
            _logger.Records.ShouldContain(record => record.Message.StartsWith("Generation completed ("));
        }

        [Fact]
        public async Task Should_Run_PostGeneration_After_Generation()
        {
            SetupStoreConfig();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";

            SetupPostGenRunner(new PostGenerationCommandResult { ExitCode = 0 });
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            _logger.Records.ShouldContain(record => record.Message == "Generating diagrams...");
        }

        [Fact]
        public async Task Should_Continue_After_PostGeneration_Failure()
        {
            SetupStoreConfig();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";

            SetupPostGenRunner(new PostGenerationCommandResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 6,
                ErrorMessage = "Deploy failed"
            });

            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            // The failure itself is logged by the runner; the pipeline continues regardless.
            _logger.Records.ShouldContain(record => record.Message.StartsWith("Generation completed ("));
        }

        [Fact]
        public async Task Should_Run_Full_Pipeline_In_Order()
        {
            SetupStoreConfig(@"C:\Projects\test.sln");

            _restoreSolution.Value = true;
            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";
            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";

            SetupRestoreRunner(new RestoreSolutionResult { ExitCode = 0 });
            SetupPreGenRunner(new PreGenerationCommandResult { ExitCode = 0 });
            SetupPostGenRunner(new PostGenerationCommandResult { ExitCode = 0 });
            SetupGenerator();

            await CollectLogsAsync(CancellationToken.None);

            var logsList = _logger.Records.ToList();
            var restoreIndex = logsList.FindIndex(record => record.Message.StartsWith("Restoring solution:"));
            var preGenIndex = logsList.FindIndex(record => record.Message == "Running pre-generation command...");
            var generationIndex = logsList.FindIndex(record => record.Message == "Generating diagrams...");
            var postGenIndex = logsList.FindIndex(record => record.Message == "Running post-generation command...");

            restoreIndex.ShouldBeGreaterThanOrEqualTo(0);
            preGenIndex.ShouldBeGreaterThan(restoreIndex);
            generationIndex.ShouldBeGreaterThan(preGenIndex);
            postGenIndex.ShouldBeGreaterThan(generationIndex);
        }

        private async Task CollectLogsAsync(CancellationToken cancellationToken)
        {
            await _service.RunAsync(cancellationToken);
        }

        private void SetupStoreConfig(string? solutionPath = null)
        {
            // Generation requires a file-backed document, so the config dump sees a non-null path.
            _store.DocumentFilePath.Returns(@"C:\Projects\test.sds");

            _store.BuildDocument().Returns(new DependencyProjectDocument
            {
                DiagramGenerator = new DependencyGeneratorConfig
                {
                    Solution = new GeneratorSolutionOptions
                    {
                        SolutionPath = solutionPath ?? string.Empty
                    }
                }
            });
        }

        private void SetupGeneratorThatThrows(Exception exception)
        {
            var generator = Substitute.For<IDependencyGenerator>();

            generator.CreateDiagramsAsync(Arg.Any<DependencyGeneratorConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(exception));

            _generatorFactory
                .ExecuteAsync(
                    Arg.Any<Func<IDependencyGenerator, CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(callInfo =>
                {
                    var operation = callInfo.Arg<Func<IDependencyGenerator, CancellationToken, Task>>();
                    return operation(generator, callInfo.ArgAt<CancellationToken>(1));
                });
        }
    }

    public class RunRestoreSolutionAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Return_True_When_Restore_Disabled()
        {
            var result = await _service.RunRestoreSolutionAsync(@"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeTrue();

            _logger.Records.ShouldHaveSingleItem();
            _logger.Records[0].Level.ShouldBe(LogLevel.Debug);
            _logger.Records[0].Message.ShouldBe("Solution restore disabled — skipping");
        }

        [Fact]
        public async Task Should_Return_True_When_Restore_Succeeds()
        {
            _restoreSolution.Value = true;

            SetupRestoreRunner(new RestoreSolutionResult { ExitCode = 0 });

            var result = await _service.RunRestoreSolutionAsync(@"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Return_False_When_No_Solution_Path_Configured()
        {
            _restoreSolution.Value = true;

            var result = await _service.RunRestoreSolutionAsync(string.Empty, CancellationToken.None);

            result.ShouldBeFalse();

            _logger.Records.ShouldContain(record =>
                record.Level == LogLevel.Error && record.Message == "Solution restore failed: no solution path is configured.");
        }

        [Fact]
        public async Task Should_Return_False_When_Restore_Fails()
        {
            _restoreSolution.Value = true;

            SetupRestoreRunner(new RestoreSolutionResult
            {
                ErrorCode = CommandErrorCode.ProcessExitedWithFailure,
                ExitCode = 4,
                ErrorMessage = "Restore failed"
            });

            var result = await _service.RunRestoreSolutionAsync(@"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeFalse();
        }
    }

    public class RunPostGenerationAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Not_Run_When_Disabled()
        {
            await _service.RunPostGenerationAsync(CancellationToken.None);

            _logger.Records.ShouldHaveSingleItem();
            _logger.Records[0].Level.ShouldBe(LogLevel.Debug);
            _logger.Records[0].Message.ShouldBe("Post-generation command disabled or has no command — skipping");

            await _postGenRunnerFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, TestContext.Current.CancellationToken);
        }
    }

    private void SetupPreGenRunner(PreGenerationCommandResult result)
    {
        _runnerFactory
            .ExecuteAsync(
                Arg.Any<Func<IPreGenerationCommandRunner, CancellationToken, Task<PreGenerationCommandResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var operation = callInfo.Arg<Func<IPreGenerationCommandRunner, CancellationToken, Task<PreGenerationCommandResult>>>();
                var runner = Substitute.For<IPreGenerationCommandRunner>();

                runner.StdOut.Returns(Observable.Empty<string>());
                runner.StdErr.Returns(Observable.Empty<string>());
                runner.RunAsync(Arg.Any<PreGenerationConfig>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

                return operation(runner, callInfo.ArgAt<CancellationToken>(1));
            });
    }

    private void SetupRestoreRunner(RestoreSolutionResult result)
    {
        _restoreRunnerFactory
            .ExecuteAsync(
                Arg.Any<Func<IRestoreSolutionRunner, CancellationToken, Task<RestoreSolutionResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var operation = callInfo.Arg<Func<IRestoreSolutionRunner, CancellationToken, Task<RestoreSolutionResult>>>();
                var runner = Substitute.For<IRestoreSolutionRunner>();

                runner.StdOut.Returns(Observable.Empty<string>());
                runner.StdErr.Returns(Observable.Empty<string>());
                runner.RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

                return operation(runner, callInfo.ArgAt<CancellationToken>(1));
            });
    }

    private void SetupPostGenRunner(PostGenerationCommandResult result)
    {
        _postGenRunnerFactory
            .ExecuteAsync(
                Arg.Any<Func<IPostGenerationCommandRunner, CancellationToken, Task<PostGenerationCommandResult>>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var operation = callInfo.Arg<Func<IPostGenerationCommandRunner, CancellationToken, Task<PostGenerationCommandResult>>>();
                var runner = Substitute.For<IPostGenerationCommandRunner>();

                runner.StdOut.Returns(Observable.Empty<string>());
                runner.StdErr.Returns(Observable.Empty<string>());
                runner.RunAsync(Arg.Any<PostGenerationConfig>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

                return operation(runner, callInfo.ArgAt<CancellationToken>(1));
            });
    }

    private void SetupGenerator()
    {
        var generator = Substitute.For<IDependencyGenerator>();

        _generatorFactory
            .ExecuteAsync(
                Arg.Any<Func<IDependencyGenerator, CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                var operation = callInfo.Arg<Func<IDependencyGenerator, CancellationToken, Task>>();
                return operation(generator, callInfo.ArgAt<CancellationToken>(1));
            });
    }
}
