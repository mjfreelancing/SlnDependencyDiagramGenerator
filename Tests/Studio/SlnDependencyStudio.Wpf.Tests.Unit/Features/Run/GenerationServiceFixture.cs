using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;
using SlnDependencyStudio.Shared.PreGeneration;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Run;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Run;

[Collection(nameof(ReactiveUIInitializer))]
public class GenerationServiceFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IScopedOperationFactory<IPreGenerationCommandRunner> _runnerFactory = Substitute.For<IScopedOperationFactory<IPreGenerationCommandRunner>>();
    private readonly IScopedOperationFactory<IDependencyGenerator> _generatorFactory = Substitute.For<IScopedOperationFactory<IDependencyGenerator>>();
    private readonly ILogger<GenerationService> _logger = NullLogger<GenerationService>.Instance;

    private readonly TrackableValue<bool> _preGenEnabled = new();
    private readonly TrackableValue<string> _preGenCommand = new();
    private readonly TrackableValue<string> _preGenArguments = new();
    private readonly TrackableValue<string> _preGenWorkingDirectory = new();
    private readonly TrackableValue<bool> _preGenContinueOnFailure = new();
    private readonly IPreGenerationConfigEditor _preGenEditor = Substitute.For<IPreGenerationConfigEditor>();

    private readonly GenerationService _service;

    public GenerationServiceFixture()
    {
        _preGenEnabled.SetOriginalValue(false);
        _preGenCommand.SetOriginalValue(string.Empty);
        _preGenArguments.SetOriginalValue(string.Empty);
        _preGenWorkingDirectory.SetOriginalValue(string.Empty);
        _preGenContinueOnFailure.SetOriginalValue(false);

        _preGenEditor.Enabled.Returns(_preGenEnabled);
        _preGenEditor.Command.Returns(_preGenCommand);
        _preGenEditor.Arguments.Returns(_preGenArguments);
        _preGenEditor.WorkingDirectory.Returns(_preGenWorkingDirectory);
        _preGenEditor.ContinueOnFailure.Returns(_preGenContinueOnFailure);
        _store.PreGenerationEditor.Returns(_preGenEditor);

        _service = new GenerationService(_store, _runnerFactory, _generatorFactory, _logger);
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

        [Fact]
        public async Task Should_Stream_Progress_From_Generator()
        {
            var observer = CreateObserver();
            var generator = Substitute.For<IDependencyGenerator>();

            generator.OnProgress.Returns(Observable.Return("Processing LibA"));

            _generatorFactory
                .ExecuteAsync(
                    Arg.Any<Func<IDependencyGenerator, CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var operation = callInfo.Arg<Func<IDependencyGenerator, CancellationToken, Task>>();
                    return operation(generator, callInfo.Arg<CancellationToken>());
                });

            var config = new DependencyGeneratorConfig();

            await _service.RunDiagramGenerationAsync(observer, config, CancellationToken.None);

            observer.Messages.ShouldContain(message => message.Text == "Processing LibA" && message.Level == OutputMessageLevel.Information);
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

            messages.ShouldContain(m => m.Text == "=== Generation Started ===");
            messages.ShouldContain(m => m.Text.StartsWith("=== Generation Completed ("));
            messages.Last().Level.ShouldBe(OutputMessageLevel.Information);
        }

        [Fact]
        public async Task Should_Stream_Progress_From_Generator()
        {
            SetupStoreConfig();

            var generator = Substitute.For<IDependencyGenerator>();
            generator.OnProgress.Returns(Observable.Return("Processing LibA"));

            _generatorFactory
                .ExecuteAsync(
                    Arg.Any<Func<IDependencyGenerator, CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(callInfo =>
                {
                    var operation = callInfo.Arg<Func<IDependencyGenerator, CancellationToken, Task>>();
                    return operation(generator, callInfo.ArgAt<CancellationToken>(1));
                });

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(m => m.Text == "Processing LibA" && m.Level == OutputMessageLevel.Information);
        }

        [Fact]
        public async Task Should_Emit_Error_When_Generator_Throws()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new InvalidOperationException("Something went wrong"));

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(m => m.Text == "Generation failed: Something went wrong"
                                        && m.Level == OutputMessageLevel.Error);
        }

        [Fact]
        public async Task Should_Emit_Cancelled_When_OperationCanceledException()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new OperationCanceledException());

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(m => m.Text == "Generation cancelled"
                                        && m.Level == OutputMessageLevel.Warning);
        }

        [Fact]
        public async Task Should_Emit_Timeout_Error_When_TimeoutException()
        {
            SetupStoreConfig();
            SetupGeneratorThatThrows(new TimeoutException("Timed out"));

            var messages = await CollectMessagesAsync(CancellationToken.None);

            messages.ShouldContain(m => m.Text == "Generation timed out: Timed out"
                                        && m.Level == OutputMessageLevel.Error);
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

            messages.ShouldContain(m => m.Text == "Pre-generation command completed successfully");
            messages.ShouldContain(m => m.Text == "Generating diagrams…");
            messages.ShouldContain(m => m.Text.StartsWith("=== Generation Completed ("));
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

            messages.ShouldContain(m => m.Text == "Pre-generation command failed: Build failed"
                                        && m.Level == OutputMessageLevel.Error);
            messages.ShouldNotContain(m => m.Text == "Generating diagrams…");
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

            messages.ShouldContain(m => m.Text.Contains("continuing"));
            messages.ShouldContain(m => m.Text == "Generating diagrams…");
            messages.ShouldContain(m => m.Text.StartsWith("=== Generation Completed ("));
        }

        [Fact]
        public async Task Should_Build_Config_From_Store()
        {
            SetupStoreConfig();
            SetupGenerator();

            await CollectMessagesAsync(CancellationToken.None);

            _store.Received(1).BuildGeneratorConfig();
        }

        private async Task<IList<OutputMessage>> CollectMessagesAsync(CancellationToken cancellationToken)
        {
            return await _service.RunAsync(cancellationToken).ToList();
        }

        private void SetupStoreConfig()
        {
            _store.BuildGeneratorConfig().Returns(new DependencyGeneratorConfig());
        }

        private void SetupGeneratorThatThrows(Exception exception)
        {
            var generator = Substitute.For<IDependencyGenerator>();
            generator.OnProgress.Returns(Observable.Empty<string>());
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

    private void SetupGenerator()
    {
        var generator = Substitute.For<IDependencyGenerator>();
        generator.OnProgress.Returns(Observable.Empty<string>());

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
