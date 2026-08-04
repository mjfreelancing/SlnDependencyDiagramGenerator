using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog.Events;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Logging;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

namespace SlnDependencyStudio.Wpf.Features.Output;

/// <summary>
/// View model for the output panel at the bottom of the main window.
/// Displays messages from analysis/generation sessions. Log events are captured by an
/// <see cref="IStudioLogBuffer"/> from application start; the buffer replays the pre-subscription
/// snapshot when this view model subscribes, then streams live events. The <see cref="IsVerbose"/>
/// toggle is a display filter that applies only to new entries; the snapshot is filtered by the
/// restored preference. Preferences are persisted under <c>ApplicationSettings.Output</c>.
/// </summary>
public sealed class OutputPanelViewModel : ReactiveObject, IStudioScopedDependency, IDisposable
{
    // Matches the timestamp portion of the Serilog file sink's default output template
    // ({Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}) so the output panel agrees with the log file.
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

    private readonly IStudioLogBuffer _logBuffer;
    private readonly IApplicationSettingsService _applicationSettings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<OutputPanelViewModel> _logger;
    private readonly IDisposable _sinkSubscription;
    private LogEventLevel _minDisplayLevel = LogEventLevel.Information;
    private bool _initializing;

    private bool _isVerbose;
    private bool _wrapContent;
    private bool _autoScroll;
    private bool _isOperationRunning;
    private bool _canCancel;

    /// <summary>The messages displayed in the output panel.</summary>
    public ObservableCollection<OutputMessage> Messages { get; } = [];

    /// <summary>Command that clears the output panel.</summary>
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    /// <summary>Command that cancels the currently running operation (analysis or generation).</summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Command that copies all output panel text to the clipboard.</summary>
    public ReactiveCommand<Unit, Unit> CopyAllCommand { get; }

    /// <summary>Command that saves all output panel text to a file. Uses an interaction to prompt for the save path.</summary>
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    /// <summary>Interaction that prompts the user to choose a save-file path. Returns the chosen path, or null if cancelled.</summary>
    public Interaction<Unit, string?> SaveFileDialog { get; } = new();

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
    /// <see langword="true"/> when the current operation can be cancelled.
    /// Set to <see langword="false"/> once cancellation has been requested.
    /// </summary>
    public bool CanCancel
    {
        get => _canCancel;
        set => this.RaiseAndSetIfChanged(ref _canCancel, value);
    }

    /// <summary>
    /// When <see langword="true"/>, <see cref="LogEventLevel.Debug"/> events are shown in the
    /// output panel. The toggle is a display filter that applies only to new entries; existing
    /// messages are not re-filtered. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IsVerbose
    {
        get => _isVerbose;
        set
        {
            this.RaiseAndSetIfChanged(ref _isVerbose, value);

            _minDisplayLevel = value
                ? LogEventLevel.Debug
                : LogEventLevel.Information;

            _logger.LogDebug("Verbose logging {State}", value ? "enabled" : "disabled");

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
    /// <param name="logBuffer">The log buffer that captures and streams log events.</param>
    /// <param name="applicationSettings">The application settings service for persisting preferences.</param>
    /// <param name="fileSystem">The file system abstraction for saving output.</param>
    /// <param name="logger">The logger instance.</param>
    public OutputPanelViewModel(IStudioLogBuffer logBuffer, IApplicationSettingsService applicationSettings,
        IFileSystem fileSystem, ILogger<OutputPanelViewModel> logger)
    {
        _logBuffer = logBuffer;
        _applicationSettings = applicationSettings;
        _fileSystem = fileSystem;
        _logger = logger;

        // Self-referencing — Messages is owned by this ViewModel.
        var hasContent = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => Messages.CollectionChanged += handler,
                handler => Messages.CollectionChanged -= handler)
            .Select(_ => Messages.Count > 0)
            .StartWith(Messages.Count > 0);

        ClearCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogDebug("Output panel cleared");
            Messages.Clear();
        }, hasContent);

        var canCancel = this.WhenAnyValue(vm => vm.CanCancel);
        CancelCommand = ReactiveCommand.Create(() => { }, canCancel);

        CopyAllCommand = ReactiveCommand.Create(() =>
        {
            var text = string.Join(Environment.NewLine, Messages.Select(message => message.Text));
            Clipboard.SetText(text);

            _logger.LogDebug("Output copied to clipboard");
        }, hasContent);

        SaveAsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var filePath = await SaveFileDialog.Handle(Unit.Default);

            if (filePath is not null)
            {
                var text = string.Join(Environment.NewLine, Messages.Select(message => message.Text));
                await _fileSystem.WriteAllTextAsync(filePath, text);

                _logger.LogDebug("Output saved to {FilePath}", filePath);
            }
        }, hasContent);

        // Restore the persisted verbose preference before subscribing so the buffered snapshot is
        // filtered by the stored toggle.
        RestorePreferences();

        // Subscribe to the log buffer. It replays the pre-subscription snapshot captured from
        // application start, then streams live events. The display filter (IsVerbose) controls
        // which levels are added to Messages — the buffer itself always captures at Debug.
        _sinkSubscription = _logBuffer
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(OnLogEntry);
    }

    private void OnLogEntry(StudioLogEntry entry)
    {
        if (entry.Level >= _minDisplayLevel)
        {
            Messages.Add(MapToOutputMessage(entry));
        }
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

    /// <inheritdoc />
    public void Dispose()
    {
        _sinkSubscription.Dispose();
    }

    private static OutputMessage MapToOutputMessage(StudioLogEntry entry)
    {
        var level = entry.Level switch
        {
            LogEventLevel.Error or LogEventLevel.Fatal => OutputMessageLevel.Error,
            LogEventLevel.Warning => OutputMessageLevel.Warning,
            _ => OutputMessageLevel.Information
        };

        var timestamp = FormatTimestamp(entry.Timestamp);

        var text = entry.ExceptionText is null
            ? $"{timestamp} {entry.Message}"
            : $"{timestamp} {entry.Message}{Environment.NewLine}{entry.ExceptionText}";

        return new OutputMessage
        {
            Text = text,
            Level = level
        };
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        // Uses the same timestamp format as the file sink so the UI matches the log file.
        // The format is culture-neutral, so rendering through the current culture is safe.
        // No timezone conversion is applied — the timestamp is shown as recorded (local time).
        return timestamp.ToString(TimestampFormat, CultureInfo.CurrentCulture);
    }
}
