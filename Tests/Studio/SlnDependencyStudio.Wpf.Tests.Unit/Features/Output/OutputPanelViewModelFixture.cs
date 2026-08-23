using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog.Events;
using Shouldly;
using SlnDependencyStudio.Shared.Logging;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Globalization;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Output;

[Collection(nameof(ReactiveUIInitializer))]
public class OutputPanelViewModelFixture
{
    private readonly IApplicationSettingsService _appSettings = Substitute.For<IApplicationSettingsService>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ILogger<OutputPanelViewModel> _logger = Substitute.For<ILogger<OutputPanelViewModel>>();
    private readonly ApplicationSettings _settings = new();
    private readonly StudioLogBuffer _logBuffer = new();
    private readonly OutputPanelViewModel _viewModel;

    public OutputPanelViewModelFixture()
    {
        _appSettings.CurrentSettings.Returns(_settings);
        _appSettings.CurrentState.Returns(new ApplicationState());

        _viewModel = new OutputPanelViewModel(_logBuffer, _appSettings, _fileSystem, Substitute.For<IErrorDialogService>(), _logger);
    }

    public class Construction : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Have_Empty_Messages()
        {
            _viewModel.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Have_ClearCommand()
        {
            _viewModel.ClearCommand.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Restore_Defaults_From_Settings()
        {
            _viewModel.IsVerbose.ShouldBeFalse();
            _viewModel.WrapContent.ShouldBeFalse();
        }

        [Fact]
        public void Should_Restore_WrapContent_From_Settings()
        {
            _settings.Output.WrapContent = true;

            var vm = new OutputPanelViewModel(_logBuffer, _appSettings, _fileSystem, Substitute.For<IErrorDialogService>(), _logger);

            vm.WrapContent.ShouldBeTrue();
        }

        [Fact]
        public void Should_Restore_IsVerbose_From_Settings()
        {
            _settings.Output.IsVerboseLogging = false;

            var vm = new OutputPanelViewModel(_logBuffer, _appSettings, _fileSystem, Substitute.For<IErrorDialogService>(), _logger);

            vm.IsVerbose.ShouldBeFalse();
        }
    }

    public class ClearCommand : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Clear_Messages()
        {
            _viewModel.Messages.Add(new OutputMessage { Text = "test" });

            _viewModel.ClearCommand.Execute().Subscribe();

            _viewModel.Messages.ShouldBeEmpty();
        }
    }

    public class IsVerbose : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Persist_When_Toggled()
        {
            _viewModel.IsVerbose = false;

            _settings.Output.IsVerboseLogging.ShouldBeFalse();

            // The view model explicitly passes CancellationToken.None to SaveSettingsAsync.
            _appSettings.Received(1).SaveSettingsAsync(CancellationToken.None);
        }

        [Fact]
        public void Should_Not_Persist_During_Initialization()
        {
            // Persistence is not called during construction because _initializing is true.
            // This is verified indirectly: the constructor does not throw and
            // _appSettings.SaveSettingsAsync is not called.
            _appSettings.DidNotReceive().SaveSettingsAsync(Arg.Any<CancellationToken>());
        }
    }

    public class WrapContent : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Persist_When_Toggled()
        {
            _viewModel.WrapContent = true;

            _settings.Output.WrapContent.ShouldBeTrue();

            // The view model explicitly passes CancellationToken.None to SaveSettingsAsync.
            _appSettings.Received(1).SaveSettingsAsync(CancellationToken.None);
        }
    }

    public class LogBufferSubscription : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Add_Live_Events()
        {
            _logBuffer.Add(CreateEntry(LogEventLevel.Information, "Test message"));

            _viewModel.Messages.Count.ShouldBe(1);
            _viewModel.Messages[0].Text.ShouldEndWith("Test message");
        }

        [Fact]
        public void Should_Replay_Backlog_From_Before_Subscription()
        {
            var logBuffer = new StudioLogBuffer();
            logBuffer.Add(CreateEntry(LogEventLevel.Information, "early message"));

            var vm = new OutputPanelViewModel(logBuffer, _appSettings, _fileSystem, Substitute.For<IErrorDialogService>(), _logger);

            vm.Messages.Count.ShouldBe(1);
            vm.Messages[0].Text.ShouldEndWith("early message");
        }

        [Fact]
        public void Should_Filter_Snapshot_By_Persisted_Verbose_Preference()
        {
            _settings.Output.IsVerboseLogging = true;

            var logBuffer = new StudioLogBuffer();
            logBuffer.Add(CreateEntry(LogEventLevel.Debug, "debug message"));

            var vm = new OutputPanelViewModel(logBuffer, _appSettings, _fileSystem, Substitute.For<IErrorDialogService>(), _logger);

            vm.Messages.Count.ShouldBe(1);
            vm.Messages[0].Text.ShouldEndWith("debug message");
        }

        [Fact]
        public void Should_Color_Errors_As_Error()
        {
            _logBuffer.Add(CreateEntry(LogEventLevel.Error, "Error message"));

            _viewModel.Messages[0].Level.ShouldBe(OutputMessageLevel.Error);
        }

        [Fact]
        public void Should_Color_Warnings_As_Warning()
        {
            _logBuffer.Add(CreateEntry(LogEventLevel.Warning, "Warning message"));

            _viewModel.Messages[0].Level.ShouldBe(OutputMessageLevel.Warning);
        }

        [Fact]
        public void Should_Color_Debug_As_Debug()
        {
            _viewModel.IsVerbose = true;

            _logBuffer.Add(CreateEntry(LogEventLevel.Debug, "Debug message"));

            _viewModel.Messages[0].Level.ShouldBe(OutputMessageLevel.Debug);
        }

        [Fact]
        public void Should_Hide_Debug_When_Verbose_Off()
        {
            _logBuffer.Add(CreateEntry(LogEventLevel.Debug, "Debug message"));

            _viewModel.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Show_Debug_When_Verbose_On()
        {
            _viewModel.IsVerbose = true;

            _logBuffer.Add(CreateEntry(LogEventLevel.Debug, "Debug message"));

            _viewModel.Messages.Count.ShouldBe(1);
            _viewModel.Messages[0].Text.ShouldEndWith("Debug message");
        }

        [Fact]
        public void Should_Not_Refilter_Existing_Messages_When_Toggled()
        {
            _logBuffer.Add(CreateEntry(LogEventLevel.Information, "info message"));

            _viewModel.Messages.Count.ShouldBe(1);

            _viewModel.IsVerbose = true;

            // Existing messages are not re-filtered or removed.
            _viewModel.Messages.Count.ShouldBe(1);
            _viewModel.Messages[0].Text.ShouldEndWith("info message");

            _viewModel.IsVerbose = false;

            // Existing messages remain even after verbose is disabled.
            _viewModel.Messages.Count.ShouldBe(1);
        }

        [Fact]
        public void Should_Filter_Only_New_Entries_When_Toggled()
        {
            _viewModel.IsVerbose = true;

            _logBuffer.Add(CreateEntry(LogEventLevel.Debug, "debug message"));

            _viewModel.Messages.Count.ShouldBe(1);

            _viewModel.IsVerbose = false;

            // A debug entry added while verbose was on remains; new debug entries are filtered.
            _viewModel.Messages.Count.ShouldBe(1);

            _logBuffer.Add(CreateEntry(LogEventLevel.Debug, "new debug message"));

            _viewModel.Messages.Count.ShouldBe(1);
        }

        [Fact]
        public void Should_Prefix_Timestamp_With_File_Format()
        {
            var originalCulture = CultureInfo.CurrentCulture;

            try
            {
                // Force a known culture so the expected output is deterministic regardless of
                // the machine's current culture.
                CultureInfo.CurrentCulture = new CultureInfo("en-AU");

                var timestamp = new DateTimeOffset(2026, 8, 4, 11, 19, 56, TimeSpan.Zero);

                _logBuffer.Add(new StudioLogEntry(timestamp, LogEventLevel.Information, "Test message", null));

                // Matches the file sink's timestamp format (yyyy-MM-dd HH:mm:ss.fff zzz).
                _viewModel.Messages[0].Text.ShouldStartWith("2026-08-04 11:19:56.000 +00:00 Test message");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        private static StudioLogEntry CreateEntry(LogEventLevel level, string message)
        {
            return new StudioLogEntry(DateTimeOffset.Now, level, message, null);
        }
    }

    public class SaveAsCommandError : OutputPanelViewModelFixture
    {
        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            var errorDialog = ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction);

            var viewModel = new OutputPanelViewModel(_logBuffer, _appSettings, _fileSystem, errorDialog, _logger);

            var exception = new InvalidOperationException("Write failed");

            _fileSystem
                .WriteAllTextAsync("output.txt", Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            interaction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            viewModel.SaveFileDialog.RegisterHandler(context => context.SetOutput("output.txt"));

            // The failure is routed to the wired ThrownExceptions handler, which shows the error dialog.
            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(viewModel.SaveAsCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Save Output failed");
            capturedError.Message.ShouldBe("Write failed");
        }
    }

    public class PersistSettingsError : OutputPanelViewModelFixture
    {
        [Fact]
        public async Task Should_Show_Error_When_Save_Fails()
        {
            var errorDialog = ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction);

            var viewModel = new OutputPanelViewModel(_logBuffer, _appSettings, _fileSystem, errorDialog, _logger);

            _appSettings
                .SaveSettingsAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Access denied"));

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            interaction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            // Toggling a preference triggers the background persist, which fails and surfaces the error.
            viewModel.IsVerbose = true;

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Save Settings failed");
            capturedError.Message.ShouldBe("The output panel settings could not be saved.\n\nAccess denied");
        }
    }
}
