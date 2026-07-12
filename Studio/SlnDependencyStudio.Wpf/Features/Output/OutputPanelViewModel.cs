using AllOverIt.Extensions;
using AllOverIt.Serilog.Sinks.Observable;
using ReactiveUI;
using Serilog.Events;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Application;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Output;

/// <summary>
/// View model for the output panel at the bottom of the main window.
/// Displays messages from analysis/generation sessions. When <see cref="IsVerbose"/>
/// is enabled, also streams all application log events from <see cref="IObservableSink"/>.
/// Preferences are persisted under <c>ApplicationSettings.Output</c>.
/// </summary>
public sealed class OutputPanelViewModel : ReactiveObject, IStudioScopedDependency
{
    private readonly IObservableSink _observableSink;
    private readonly IApplicationSettingsService _applicationSettings;
    private IDisposable? _verboseSubscription;
    private bool _initializing;

    private bool _isVerbose;
    private bool _wrapContent;
    private bool _autoScroll;
    private bool _isOperationRunning;

    /// <summary>The messages displayed in the output panel.</summary>
    public ObservableCollection<OutputMessage> Messages { get; } = [];

    /// <summary>Command that clears the output panel.</summary>
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    /// <summary>Command that cancels the currently running operation (analysis or generation).</summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// <see langword="true"/> while an analysis or generation operation is in progress.
    /// Controls the visibility of the Cancel button in the output panel header.
    /// </summary>
    public bool IsOperationRunning
    {
        get => _isOperationRunning;
        set => this.RaiseAndSetIfChanged(ref _isOperationRunning, value);
    }

    /// <summary>
    /// When <see langword="true"/>, all application log events (Information and above)
    /// also appear in the output panel. Defaults to <see langword="true"/></c>.
    /// </summary>
    public bool IsVerbose
    {
        get => _isVerbose;
        set
        {
            this.RaiseAndSetIfChanged(ref _isVerbose, value);

            if (value)
            {
                SubscribeToVerboseLog();
            }
            else
            {
                UnsubscribeFromVerboseLog();
            }

            PersistIfNotInitializing();
        }
    }

    /// <summary>
    /// When <see langword="true"/>, long lines wrap to the next line.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool WrapContent
    {
        get => _wrapContent;
        set
        {
            this.RaiseAndSetIfChanged(ref _wrapContent, value);
            PersistIfNotInitializing();
        }
    }

    /// <summary>
    /// When <see langword="true"/>, the output panel auto-scrolls to the
    /// bottom when new messages arrive. Defaults to <see langword="true"/>.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoScroll, value);
            PersistIfNotInitializing();
        }
    }

    /// <summary>Initializes a new instance of <see cref="OutputPanelViewModel"/>.</summary>
    /// <param name="observableSink">The Serilog observable sink for verbose log streaming.</param>
    /// <param name="applicationSettings">The application settings service for persisting preferences.</param>
    public OutputPanelViewModel(IObservableSink observableSink, IApplicationSettingsService applicationSettings)
    {
        _observableSink = observableSink;
        _applicationSettings = applicationSettings;

        ClearCommand = ReactiveCommand.Create(Messages.Clear);
        CancelCommand = ReactiveCommand.Create(() => { });

        RestorePreferences();
    }

    private void RestorePreferences()
    {
        _initializing = true;

        try
        {
            var output = _applicationSettings.CurrentSettings.Output;
            WrapContent = output.WrapContent;
            IsVerbose = output.IsVerboseLogging;
            AutoScroll = output.AutoScroll;
        }
        finally
        {
            _initializing = false;
        }
    }

    private void PersistIfNotInitializing()
    {
        if (_initializing)
        {
            return;
        }

        var output = _applicationSettings.CurrentSettings.Output;
        output.WrapContent = _wrapContent;
        output.IsVerboseLogging = _isVerbose;
        output.AutoScroll = _autoScroll;

        _ = _applicationSettings.SaveSettingsAsync();
    }

    private void SubscribeToVerboseLog()
    {
        _verboseSubscription?.Dispose();

        _verboseSubscription = _observableSink
            .Where(logEvent => logEvent.Level >= LogEventLevel.Information)
            .Select(MapToOutputMessage)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(Messages.Add);
    }

    private void UnsubscribeFromVerboseLog()
    {
        _verboseSubscription?.Dispose();
        _verboseSubscription = null;
    }

    private static OutputMessage MapToOutputMessage(LogEvent logEvent)
    {
        var level = logEvent.Level switch
        {
            LogEventLevel.Error or LogEventLevel.Fatal => OutputMessageLevel.Error,
            LogEventLevel.Warning => OutputMessageLevel.Warning,
            _ => OutputMessageLevel.Information
        };

        var text = logEvent.RenderMessage();

        return new OutputMessage
        {
            Text = $"[⚡] {text}",
            Level = level
        };
    }
}
