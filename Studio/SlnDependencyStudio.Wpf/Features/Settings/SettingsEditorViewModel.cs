using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>View model for the <see cref="SettingsEditor"/> control (everything except Save/Cancel,
/// which belong to <see cref="SettingsWindowViewModel"/>).
/// Uses <see cref="Interaction{TInput, TOutput}"/> for all UI dialogs — the View
/// registers handlers in its code-behind; the ViewModel has no UI dependencies.</summary>
public sealed class SettingsEditorViewModel : ReactiveObject
{
    /// <summary>The default folder for Open/Save file dialogs.</summary>
    [Reactive]
    public string DefaultProjectFolder { get; set; } = string.Empty;

    /// <summary>Explicit path override for the d2 CLI tool.</summary>
    [Reactive]
    public string D2ToolPath { get; set; } = string.Empty;

    /// <summary>Explicit path override for the mmdc CLI tool.</summary>
    [Reactive]
    public string MmdcToolPath { get; set; } = string.Empty;

    /// <summary>Number of days to retain log files.</summary>
    [Reactive]
    public int LogRetentionDays { get; set; } = 30;

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
    public SettingsEditorViewModel(IApplicationSettingsService settingsService)
    {
        var current = settingsService.CurrentSettings;

        DefaultProjectFolder = current.DefaultProjectFolder;
        D2ToolPath = GetToolPathOverride(current, "d2");
        MmdcToolPath = GetToolPathOverride(current, "mmdc");
        LogRetentionDays = current.LogRetentionDays;

        BrowseDefaultProjectFolderCommand = ReactiveCommand.CreateFromTask(BrowseDefaultProjectFolderAsync);
        BrowseD2ToolPathCommand = ReactiveCommand.CreateFromTask(BrowseD2ToolPathAsync);
        BrowseMmdcToolPathCommand = ReactiveCommand.CreateFromTask(BrowseMmdcToolPathAsync);
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
