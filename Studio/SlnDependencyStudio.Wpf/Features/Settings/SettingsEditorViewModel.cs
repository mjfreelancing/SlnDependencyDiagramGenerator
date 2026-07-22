using AllOverIt.Extensions;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Theming;
using SlnDependencyStudio.Wpf.Models;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>View model for the <see cref="SettingsEditor"/> control.</summary>
public sealed class SettingsEditorViewModel : ReactiveObject
{
    private readonly StudioTheme _originalTheme;

    private string _defaultProjectFolder = string.Empty;
    private string _d2ToolPath = string.Empty;
    private string _mmdcToolPath = string.Empty;
    private int _logRetentionDays = ApplicationSettings.DefaultLogRetentionDays;
    private bool _isDarkTheme;

    /// <summary>The default folder for Open/Save file dialogs.</summary>
    public string DefaultProjectFolder
    {
        get => _defaultProjectFolder;
        set => this.RaiseAndSetIfChanged(ref _defaultProjectFolder, value);
    }

    /// <summary>Explicit path override for the d2 CLI tool.</summary>
    public string D2ToolPath
    {
        get => _d2ToolPath;
        set => this.RaiseAndSetIfChanged(ref _d2ToolPath, value);
    }

    /// <summary>Explicit path override for the mmdc CLI tool.</summary>
    public string MmdcToolPath
    {
        get => _mmdcToolPath;
        set => this.RaiseAndSetIfChanged(ref _mmdcToolPath, value);
    }

    /// <summary>Number of days to retain log files.</summary>
    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set => this.RaiseAndSetIfChanged(ref _logRetentionDays, value);
    }

    /// <summary><see langword="true"/> when the dark theme is selected; <see langword="false"/> for light.</summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _isDarkTheme, value);
    }

    /// <summary>Interaction that asks the View to browse for a folder and return the selected path.
    /// Input is the initial folder path; output is the selected path, or <see langword="null"/> if cancelled.</summary>
    public Interaction<string, string?> BrowseFolder { get; } = new();

    /// <summary>Interaction that asks the View to browse for the d2 executable and return the selected path.
    /// Input is the current tool path override (used as the initial directory);
    /// output is the selected path, or <see langword="null"/> if cancelled.</summary>
    public Interaction<string, string?> BrowseD2Executable { get; } = new();

    /// <summary>Interaction that asks the View to browse for the mmdc executable and return the selected path.
    /// Input is the current tool path override (used as the initial directory);
    /// output is the selected path, or <see langword="null"/> if cancelled.</summary>
    public Interaction<string, string?> BrowseMmdcExecutable { get; } = new();

    /// <summary>Command that opens a folder browser for <see cref="DefaultProjectFolder"/>.</summary>
    public ReactiveCommand<Unit, Unit> BrowseDefaultProjectFolderCommand { get; }

    /// <summary>Command that opens a file browser for the d2 tool path override.</summary>
    public ReactiveCommand<Unit, Unit> BrowseD2ToolPathCommand { get; }

    /// <summary>Command that opens a file browser for the mmdc tool path override.</summary>
    public ReactiveCommand<Unit, Unit> BrowseMmdcToolPathCommand { get; }

    /// <summary>Initializes a new instance of <see cref="SettingsEditorViewModel"/>.
    /// Populates editing properties from the current durable settings.</summary>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="themeService">The theme service for live preview when the toggle changes.</param>
    public SettingsEditorViewModel(IApplicationSettingsService settingsService, IThemeService themeService)
    {
        var currentSettings = settingsService.CurrentSettings;

        // Capture the original theme so it can be reverted on Cancel.
        _originalTheme = currentSettings.Theme;

        // Take a snapshot of the current settings to populate the editing properties.
        // The editor operates on a copy of these settings.
        DefaultProjectFolder = currentSettings.DefaultProjectFolder;
        D2ToolPath = GetToolPathOverride(currentSettings, "d2");
        MmdcToolPath = GetToolPathOverride(currentSettings, "mmdc");
        LogRetentionDays = currentSettings.LogRetentionDays;
        IsDarkTheme = currentSettings.Theme == StudioTheme.Dark;

        // Apply the theme live as the user toggles — no need to wait for Save.
        var studioTheme = currentSettings.Theme;

        // Self-referencing — subscription is on 'this', collected with the ViewModel.
        // Also, this VM is a dialog child; no caller disposes it, so IDisposable would be dead code.
        this.WhenAnyValue(vm => vm.IsDarkTheme)
            .Subscribe(isDark => themeService.ApplyTheme(isDark ? StudioTheme.Dark : StudioTheme.Light));

        BrowseDefaultProjectFolderCommand = ReactiveCommand.CreateFromTask(BrowseDefaultProjectFolderAsync);
        BrowseD2ToolPathCommand = ReactiveCommand.CreateFromTask(BrowseD2ToolPathAsync);
        BrowseMmdcToolPathCommand = ReactiveCommand.CreateFromTask(BrowseMmdcToolPathAsync);
    }

    /// <summary>Copies the currently edited values to the provided <paramref name="settings"/> instance.</summary>
    /// <param name="settings">The target <see cref="ApplicationSettings"/> to write to.</param>
    public void ApplyToSettings(ApplicationSettings settings)
    {
        settings.DefaultProjectFolder = DefaultProjectFolder;
        settings.LogRetentionDays = LogRetentionDays;
        settings.Theme = IsDarkTheme ? StudioTheme.Dark : StudioTheme.Light;

        SetToolPathOverride(settings, "d2", D2ToolPath);
        SetToolPathOverride(settings, "mmdc", MmdcToolPath);
    }

    /// <summary>Returns the theme that was active when this editor was opened,
    /// before any live-preview toggling occurred.</summary>
    public StudioTheme GetOriginalTheme() => _originalTheme;

    private static void SetToolPathOverride(ApplicationSettings settings, string toolName, string path)
    {
        if (path.IsNullOrEmpty())
        {
            settings.ToolPathOverrides.Remove(toolName);
        }
        else
        {
            settings.ToolPathOverrides[toolName] = path;
        }
    }

    private async Task BrowseDefaultProjectFolderAsync()
    {
        var selectedPath = await BrowseFolder.Handle(DefaultProjectFolder);

        if (selectedPath is not null)
        {
            DefaultProjectFolder = selectedPath;
        }
    }

    private async Task BrowseD2ToolPathAsync()
    {
        var selectedPath = await BrowseD2Executable.Handle(D2ToolPath);

        if (selectedPath is not null)
        {
            D2ToolPath = selectedPath;
        }
    }

    private async Task BrowseMmdcToolPathAsync()
    {
        var selectedPath = await BrowseMmdcExecutable.Handle(MmdcToolPath);

        if (selectedPath is not null)
        {
            MmdcToolPath = selectedPath;
        }
    }

    private static string GetToolPathOverride(ApplicationSettings settings, string toolName)
    {
        return settings.ToolPathOverrides.TryGetValue(toolName, out var path) ? path : string.Empty;
    }
}
