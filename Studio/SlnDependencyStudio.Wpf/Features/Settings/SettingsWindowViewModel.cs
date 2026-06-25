using AllOverIt.Assertion;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>View model for the <see cref="SettingsWindow"/> shell.
/// Owns Save/Cancel logic and coordinates with <see cref="SettingsEditorViewModel"/> for editing state.</summary>
public sealed class SettingsWindowViewModel : ReactiveObject
{
    private readonly IApplicationSettingsService _settingsService;
    private readonly ApplicationSettings _originalSettings;

    /// <summary>The editing view model that holds current unsaved values.</summary>
    public SettingsEditorViewModel SettingsEditorViewModel { get; internal set; } = null!;

    /// <summary>Saves the current settings to disk and requests the window to close.</summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Reverts settings to the snapshot taken at dialog open and requests the window to close.</summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Initializes a new instance of <see cref="SettingsWindowViewModel"/>.
    /// Takes a snapshot of current settings so Cancel can revert.</summary>
    public SettingsWindowViewModel(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;

        var snapshot = CloneSettings(settingsService.CurrentSettings);
        _originalSettings = snapshot;

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    private async Task SaveAsync()
    {
        Throw<InvalidOperationException>.WhenNull(SettingsEditorViewModel, $"The {nameof(SettingsEditorViewModel)} has not been set");

        ApplyToSettings(SettingsEditorViewModel, _settingsService.CurrentSettings);
        await _settingsService.SaveAsync();
    }

    private void Cancel()
    {
        RevertSettings(_settingsService.CurrentSettings, _originalSettings);
    }

    private static void ApplyToSettings(SettingsEditorViewModel source, ApplicationSettings target)
    {
        target.DefaultProjectFolder = source.DefaultProjectFolder;
        target.LogRetentionDays = source.LogRetentionDays;

        SetToolPathOverride(target, "d2", source.D2ToolPath);
        SetToolPathOverride(target, "mmdc", source.MmdcToolPath);
    }

    private static void RevertSettings(ApplicationSettings target, ApplicationSettings snapshot)
    {
        target.DefaultProjectFolder = snapshot.DefaultProjectFolder;
        target.ToolPathOverrides.Clear();

        foreach (var kvp in snapshot.ToolPathOverrides)
        {
            target.ToolPathOverrides[kvp.Key] = kvp.Value;
        }

        target.LogRetentionDays = snapshot.LogRetentionDays;
    }

    private static void SetToolPathOverride(ApplicationSettings settings, string toolName, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            settings.ToolPathOverrides.Remove(toolName);
        }
        else
        {
            settings.ToolPathOverrides[toolName] = path;
        }
    }

    private static ApplicationSettings CloneSettings(ApplicationSettings source)
    {
        return new ApplicationSettings
        {
            DefaultProjectFolder = source.DefaultProjectFolder,
            ToolPathOverrides = new Dictionary<string, string>(source.ToolPathOverrides),
            LogRetentionDays = source.LogRetentionDays
        };
    }
}
