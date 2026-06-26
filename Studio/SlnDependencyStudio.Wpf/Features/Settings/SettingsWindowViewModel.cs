using AllOverIt.Assertion;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Wpf.Features.Application;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Settings;

/// <summary>View model for the <see cref="SettingsWindow"/> shell.
/// Owns Save/Cancel logic and coordinates with <see cref="SettingsEditorViewModel"/> for editing state.</summary>
public sealed class SettingsWindowViewModel : ReactiveObject
{
    // Records the subset of settings that require an application restart to take effect.
    // Built-in value equality means comparison is automatic when new properties are added.
    private sealed record RestartSensitiveSettings(int LogRetentionDays /* , string someOtherProp */);

    private readonly IApplicationSettingsService _settingsService;

    private readonly RestartSensitiveSettings _originalRestartSettings;

    // Used to track the subscription to restart-sensitive property changes. When the SettingsEditorViewModel
    // is assigned, a new subscription is created and the previous one is disposed.
    private readonly SerialDisposable _restartSubscription = new();

    /// <summary>The editing view model that holds current unsaved values.
    /// Set by <see cref="SettingsWindow"/> code-behind after construction. Starts <see langword="null"/>
    /// — <see cref="BeginRestartTracking"/> is triggered automatically when a value is assigned.</summary>
    [Reactive]
    public SettingsEditorViewModel? SettingsEditorViewModel { get; set; }

    /// <summary>Indicates that one or more settings have been changed that will only
    /// take effect after the application is restarted.</summary>
    [Reactive]
    public bool IsRestartRequired { get; set; }

    /// <summary>Saves the current settings to disk and requests the window to close.</summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Closes the window without saving. The editor operates on a view model
    /// populated from current settings and never mutates them directly — no revert needed.</summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Initializes a new instance of <see cref="SettingsWindowViewModel"/>.
    /// Captures the original restart-sensitive values for comparison during editing.</summary>
    public SettingsWindowViewModel(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;

        _originalRestartSettings = new(settingsService.CurrentSettings.LogRetentionDays);

        // Save edited values back to the current settings and persist to disk.
        // The window code-behind subscribes to this command and closes the window when executed.
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);

        // Do nothing — the window code-behind subscribes to this command and closes the window when executed.
        CancelCommand = ReactiveCommand.Create(() => { });

        // When the window code-behind assigns SettingsEditorViewModel, wire up restart tracking.
        this.WhenAnyValue(x => x.SettingsEditorViewModel)
            .Where(x => x is not null)
            .Subscribe(_ => BeginRestartTracking());
    }

    private async Task SaveAsync()
    {
        Throw<InvalidOperationException>.WhenNull(SettingsEditorViewModel, $"The {nameof(SettingsEditorViewModel)} has not been set");

        // The settings editor view model assigns its properties from CurrentSettings at construction.
        // The associated view binds to those properties and allows the user to edit them.
        // When Save is clicked, the editor's properties are copied back to CurrentSettings.
        SettingsEditorViewModel.ApplyToSettings(_settingsService.CurrentSettings);

        // ...and persisted to disk.
        await _settingsService.SaveAsync();
    }

    private void BeginRestartTracking()
    {
        // Watch restart-sensitive properties for changes and flag if a restart is needed.
        //
        // WhenAnyValue() caters for tracking up to 12 properties.
        // SerialDisposable handles disposal of the previous subscription if this is called again.
        //
        // When adding a new restart-sensitive property:
        //   1. Add the parameter to the RestartSensitiveSettings record.
        //   2. Add the observable to WhenAnyValue.
        //   3. Pass the new value in the record constructor below.
        _restartSubscription.Disposable = this.WhenAnyValue(
                vm => vm.SettingsEditorViewModel!.LogRetentionDays,
                /* vm => vm.SettingsEditorViewModel!.SomeOtherProp */
                (retentionDays /* , string someOtherProp */) => new RestartSensitiveSettings(retentionDays /* , string someOtherProp */))
            .Select(settings => settings != _originalRestartSettings)
            .BindTo(this, x => x.IsRestartRequired);
    }
}
