using AllOverIt.Serilog.Sinks.Observable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Output;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Output;

[Collection(nameof(ReactiveUIInitializer))]
public class OutputPanelViewModelFixture
{
    private readonly IObservableSink _observableSink = Substitute.For<IObservableSink>();
    private readonly IApplicationSettingsService _appSettings = Substitute.For<IApplicationSettingsService>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ILogger<OutputPanelViewModel> _logger = Substitute.For<ILogger<OutputPanelViewModel>>();
    private readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Information);
    private readonly ApplicationSettings _settings = new();
    private readonly Subject<LogEvent> _sinkSubject = new();
    private readonly OutputPanelViewModel _viewModel;

    public OutputPanelViewModelFixture()
    {
        _appSettings.CurrentSettings.Returns(_settings);
        _appSettings.CurrentState.Returns(new ApplicationState());

        // Wire the mock observable sink to the subject so tests can push events.
        _observableSink
            .Subscribe(Arg.Any<IObserver<LogEvent>>())
            .Returns(Disposable.Empty)
            .AndDoes(callInfo => _sinkSubject.Subscribe(callInfo.Arg<IObserver<LogEvent>>()));

        _viewModel = new OutputPanelViewModel(_observableSink, _levelSwitch, _appSettings, _fileSystem, _logger);
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

            var vm = new OutputPanelViewModel(_observableSink, _levelSwitch, _appSettings, _fileSystem, _logger);

            vm.WrapContent.ShouldBeTrue();
        }

        [Fact]
        public void Should_Restore_IsVerbose_From_Settings()
        {
            _settings.Output.IsVerboseLogging = false;

            var vm = new OutputPanelViewModel(_observableSink, _levelSwitch, _appSettings, _fileSystem, _logger);

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
            _appSettings.Received(1).SaveSettingsAsync();
        }

        [Fact]
        public void Should_Not_Persist_During_Initialization()
        {
            // Persistence is not called during construction because _initializing is true.
            // This is verified indirectly: the constructor does not throw and
            // _appSettings.SaveSettingsAsync is not called.
            _appSettings.DidNotReceive().SaveSettingsAsync();
        }
    }

    public class WrapContent : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Persist_When_Toggled()
        {
            _viewModel.WrapContent = true;

            _settings.Output.WrapContent.ShouldBeTrue();
            _appSettings.Received(1).SaveSettingsAsync();
        }
    }

    public class VerboseSubscription : OutputPanelViewModelFixture
    {
        [Fact]
        public void Should_Add_Messages_When_Events_Emitted()
        {
            var logEvent = CreateLogEvent(LogEventLevel.Information, "Test message");

            _sinkSubject.OnNext(logEvent);

            _viewModel.Messages.Count.ShouldBe(1);
            _viewModel.Messages[0].Text.ShouldBe("Test message");
        }

        [Fact]
        public void Should_Color_Errors_As_Error()
        {
            var logEvent = CreateLogEvent(LogEventLevel.Error, "Error message");

            _sinkSubject.OnNext(logEvent);

            _viewModel.Messages[0].Level.ShouldBe(OutputMessageLevel.Error);
        }

        [Fact]
        public void Should_Color_Warnings_As_Warning()
        {
            var logEvent = CreateLogEvent(LogEventLevel.Warning, "Warning message");

            _sinkSubject.OnNext(logEvent);

            _viewModel.Messages[0].Level.ShouldBe(OutputMessageLevel.Warning);
        }

        [Fact]
        public void Should_Update_LevelSwitch_When_Toggled()
        {
            // Default settings have IsVerboseLogging = false, so the switch starts at Information.
            _levelSwitch.MinimumLevel.ShouldBe(LogEventLevel.Information);

            _viewModel.IsVerbose = true;

            _levelSwitch.MinimumLevel.ShouldBe(LogEventLevel.Debug);

            _viewModel.IsVerbose = false;

            _levelSwitch.MinimumLevel.ShouldBe(LogEventLevel.Information);
        }

        private static LogEvent CreateLogEvent(LogEventLevel level, string message)
        {
            var template = new Serilog.Parsing.MessageTemplateParser().Parse(message);

            return new LogEvent(DateTimeOffset.Now, level, null, template, []);
        }
    }
}
