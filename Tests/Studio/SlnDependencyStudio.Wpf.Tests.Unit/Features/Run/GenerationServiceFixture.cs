using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;
using SlnDependencyStudio.Shared.Services;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Run;
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
    private readonly ILogger<GenerationService> _logger = NullLogger<GenerationService>.Instance;

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
            var observer = CreateObserver();

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeTrue();
            observer.Messages.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Return_True_When_Command_Is_Empty()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeTrue();
            observer.Messages.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Return_True_On_Success()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = true,
                ExitCode = 0
            });

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeTrue();
            observer.Messages.ShouldContain(message => message.Text == "Pre-generation command completed successfully");
        }

        [Fact]
        public async Task Should_Return_False_When_ContinueOnFailure_Is_False()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeFalse();

        }

        [Fact]
        public async Task Should_Return_True_When_ContinueOnFailure_Is_True()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            _preGenContinueOnFailure.Value = true; SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Return_False_When_Cancelled()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = StudioExitCode.PreGenerationCommandCancelled.Value,
                ErrorMessage = "Cancelled"
            });

            var result = await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            result.ShouldBeFalse();
            observer.Messages.ShouldContain(message => message.Text.Contains("cancelled"));
        }

        [Fact]
        public async Task Should_Resolve_Relative_WorkingDirectory()
        {
            var observer = CreateObserver();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";
            _preGenWorkingDirectory.Value = "build";
            _store.DocumentFilePath.Returns(@"C:\Projects\myproject.sds");

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = true,
                ExitCode = 0
            });

            await _service.RunPreGenerationAsync(observer, CancellationToken.None);

            await _runnerFactory.Received(1).ExecuteAsync(
                Arg.Any<Func<IPreGenerationCommandRunner, CancellationToken, Task<PreGenerationCommandResult>>>(),
                Arg.Any<CancellationToken>());
        }
    }

    public class RunDiagramGenerationAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Emit_Generating_Message()
        {
            var observer = CreateObserver();

            SetupGenerator();

            var config = new DependencyGeneratorConfig();

            await _service.RunDiagramGenerationAsync(observer, config, CancellationToken.None);

            observer.Messages.ShouldContain(message => message.Text == "Generating diagrams…");
        }

    }

    public class RunAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Emit_Started_And_Completed_When_Successful()
        {
            SetupStoreConfig();
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "=== Generation Started ===");
            messages.ShouldContain(message => message.Text.StartsWith("=== Generation Completed ("));
            messages.Last().Level.ShouldBe(OutputMessageLevel.Information);
        }

        [Fact]
        public async Task Should_Emit_Error_When_Generator_Throws()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new InvalidOperationException("Something went wrong"));

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Generation failed: Something went wrong" && message.Level == OutputMessageLevel.Error);
        }

        [Fact]
        public async Task Should_Emit_Cancelled_When_OperationCanceledException()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new OperationCanceledException());

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Generation cancelled" && message.Level == OutputMessageLevel.Warning);
        }

        [Fact]
        public async Task Should_Emit_Timeout_Error_When_TimeoutException()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new TimeoutException("Timed out"));

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Generation timed out: Timed out" && message.Level == OutputMessageLevel.Error);
        }

        [Fact]
        public async Task Should_Run_Generator_After_PreGen_Success()
        {
            SetupStoreConfig();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult { Succeeded = true, ExitCode = 0 });
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Pre-generation command completed successfully");
            messages.ShouldContain(message => message.Text == "Generating diagrams…");
            messages.ShouldContain(message => message.Text.StartsWith("=== Generation Completed ("));
        }

        [Fact]
        public async Task Should_Abort_When_PreGen_Fails_Without_Continue()
        {
            SetupStoreConfig();

            _preGenEnabled.Value = true;
            _preGenCommand.Value = "dotnet build";

            SetupPreGenRunner(new PreGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Pre-generation command failed: Build failed"
                                        && message.Level == OutputMessageLevel.Error);
            messages.ShouldNotContain(message => message.Text == "Generating diagrams…");
            _ = _generatorFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
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
                Succeeded = false,
                ExitCode = 3,
                ErrorMessage = "Build failed"
            });

            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text.Contains("continuing"));
            messages.ShouldContain(message => message.Text == "Generating diagrams…");
            messages.ShouldContain(message => message.Text.StartsWith("=== Generation Completed ("));
        }

        [Fact]
        public async Task Should_Build_Config_From_Store()
        {
            SetupStoreConfig();
            SetupGenerator();

            await CollectMessagesAsync(CancellationToken.None);

            _store.Received(1).BuildGeneratorConfig();
        }

        [Fact]
        public async Task Should_Abort_When_Restore_Fails()
        {
            SetupStoreConfig(@"C:\Projects\test.sln");

            _restoreSolution.Value = true;
            SetupRestoreRunner(new RestoreSolutionResult
            {
                Succeeded = false,
                ExitCode = 4,
                ErrorMessage = "Restore failed"
            });

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Solution restore failed: Restore failed"
                                        && message.Level == OutputMessageLevel.Error);
            messages.ShouldNotContain(message => message.Text == "Generating diagrams…");
            _ = _generatorFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
        }

        [Fact]
        public async Task Should_Run_Generator_After_Restore_Success()
        {
            SetupStoreConfig(@"C:\Projects\test.sln");

            _restoreSolution.Value = true;
            SetupRestoreRunner(new RestoreSolutionResult { Succeeded = true, ExitCode = 0 });
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Solution restore completed successfully");
            messages.ShouldContain(message => message.Text == "Generating diagrams…");
            messages.ShouldContain(message => message.Text.StartsWith("=== Generation Completed ("));
        }

        [Fact]
        public async Task Should_Run_PostGeneration_After_Generation()
        {
            SetupStoreConfig();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";
            SetupPostGenRunner(new PostGenerationCommandResult { Succeeded = true, ExitCode = 0 });
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Generating diagrams…");
            messages.ShouldContain(message => message.Text == "Post-generation command completed successfully");
        }

        [Fact]
        public async Task Should_Report_PostGeneration_Failure_As_Warning()
        {
            SetupStoreConfig();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";
            SetupPostGenRunner(new PostGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 6,
                ErrorMessage = "Deploy failed"
            });
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(message => message.Text == "Post-generation command failed: Deploy failed"
                                        && message.Level == OutputMessageLevel.Warning);
            messages.ShouldContain(message => message.Text.StartsWith("=== Generation Completed ("));
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

            SetupRestoreRunner(new RestoreSolutionResult { Succeeded = true, ExitCode = 0 });
            SetupPreGenRunner(new PreGenerationCommandResult { Succeeded = true, ExitCode = 0 });
            SetupPostGenRunner(new PostGenerationCommandResult { Succeeded = true, ExitCode = 0 });
            SetupGenerator();

            var messages = await CollectMessagesAsync(CancellationToken.None);

            var messagesList = messages.ToList();
            var restoreIndex = messagesList.FindIndex(message => message.Text == "Restoring solution…");
            var preGenIndex = messagesList.FindIndex(message => message.Text == "Running pre-generation command…");
            var generationIndex = messagesList.FindIndex(message => message.Text == "Generating diagrams…");
            var postGenIndex = messagesList.FindIndex(message => message.Text == "Running post-generation command…");

            restoreIndex.ShouldBeGreaterThanOrEqualTo(0);
            preGenIndex.ShouldBeGreaterThan(restoreIndex);
            generationIndex.ShouldBeGreaterThan(preGenIndex);
            postGenIndex.ShouldBeGreaterThan(generationIndex);
        }

        private async Task<IList<OutputMessage>> CollectMessagesAsync(CancellationToken cancellationToken)
        {
            return await _service.RunAsync(cancellationToken).ToList();
        }

        private void SetupStoreConfig(string? solutionPath = null)
        {
            _store.BuildGeneratorConfig().Returns(new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionPath ?? string.Empty
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
            var observer = CreateObserver();

            var result = await _service.RunRestoreSolutionAsync(observer, @"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeTrue();
            observer.Messages.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Return_True_When_Restore_Succeeds()
        {
            var observer = CreateObserver();

            _restoreSolution.Value = true;
            SetupRestoreRunner(new RestoreSolutionResult { Succeeded = true, ExitCode = 0 });

            var result = await _service.RunRestoreSolutionAsync(observer, @"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeTrue();
            observer.Messages.ShouldContain(message => message.Text == "Solution restore completed successfully");
        }

        [Fact]
        public async Task Should_Return_False_When_No_Solution_Path_Configured()
        {
            var observer = CreateObserver();

            _restoreSolution.Value = true;

            var result = await _service.RunRestoreSolutionAsync(observer, string.Empty, CancellationToken.None);

            result.ShouldBeFalse();
            observer.Messages.ShouldContain(message =>
                message.Text == "Solution restore failed: no solution path is configured."
                && message.Level == OutputMessageLevel.Error);
        }

        [Fact]
        public async Task Should_Return_False_When_Restore_Fails()
        {
            var observer = CreateObserver();

            _restoreSolution.Value = true;
            SetupRestoreRunner(new RestoreSolutionResult
            {
                Succeeded = false,
                ExitCode = 4,
                ErrorMessage = "Restore failed"
            });

            var result = await _service.RunRestoreSolutionAsync(observer, @"C:\Projects\test.sln", CancellationToken.None);

            result.ShouldBeFalse();
            observer.Messages.ShouldContain(message =>
                message.Text == "Solution restore failed: Restore failed"
                && message.Level == OutputMessageLevel.Error);
        }
    }

    public class RunPostGenerationAsync : GenerationServiceFixture
    {
        [Fact]
        public async Task Should_Not_Run_When_Disabled()
        {
            var observer = CreateObserver();

            await _service.RunPostGenerationAsync(observer, CancellationToken.None);

            observer.Messages.ShouldBeEmpty();
            await _postGenRunnerFactory.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default);
        }

        [Fact]
        public async Task Should_Report_Success_When_Command_Succeeds()
        {
            var observer = CreateObserver();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";
            SetupPostGenRunner(new PostGenerationCommandResult { Succeeded = true, ExitCode = 0 });

            await _service.RunPostGenerationAsync(observer, CancellationToken.None);

            observer.Messages.ShouldContain(message => message.Text == "Post-generation command completed successfully");
        }

        [Fact]
        public async Task Should_Report_Failure_As_Warning()
        {
            var observer = CreateObserver();

            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";
            SetupPostGenRunner(new PostGenerationCommandResult
            {
                Succeeded = false,
                ExitCode = 6,
                ErrorMessage = "Deploy failed"
            });

            await _service.RunPostGenerationAsync(observer, CancellationToken.None);

            observer.Messages.ShouldContain(message =>
                message.Text == "Post-generation command failed: Deploy failed"
                && message.Level == OutputMessageLevel.Warning);
        }
    }

    private static TestObserver CreateObserver() => new();

    private sealed class TestObserver : IObserver<OutputMessage>
    {
        public List<OutputMessage> Messages { get; } = [];

        public void OnNext(OutputMessage value) => Messages.Add(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
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
