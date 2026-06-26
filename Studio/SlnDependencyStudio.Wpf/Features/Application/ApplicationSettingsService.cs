using AllOverIt.Assertion;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>Default implementation of <see cref="IApplicationSettingsService"/>.
/// Persists settings to <c>%AppData%/SlnDependencyStudio/settings.json</c> using
/// <see cref="System.Text.Json"/> with an atomic write strategy.</summary>
internal sealed class ApplicationSettingsService : IApplicationSettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlnDependencyStudio");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
    private static readonly string StateFilePath = Path.Combine(SettingsDirectory, "state.json");

    private readonly IStudioJsonSerializer _jsonSerializer;

    /// <inheritdoc />
    public ApplicationSettings CurrentSettings { get; private set; } = new();

    /// <inheritdoc />
    public ApplicationState CurrentState { get; private set; } = new();

    /// <summary>Initializes a new instance of <see cref="ApplicationSettingsService"/>.</summary>
    /// <param name="jsonSerializer">The JSON serializer used to persist and load settings.</param>
    public ApplicationSettingsService(IStudioJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer.WhenNotNull();
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await LoadSettingsAsync(cancellationToken);
        await LoadStateAsync(cancellationToken);
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsFilePath))
        {
            CurrentSettings = new ApplicationSettings();
            return;
        }

        await using var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        CurrentSettings = (await _jsonSerializer.DeserializeAsync<ApplicationSettings>(stream, cancellationToken))!;
    }

    private async Task LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(StateFilePath))
        {
            CurrentState = new ApplicationState();
            return;
        }

        await using var stream = new FileStream(StateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        CurrentState = (await _jsonSerializer.DeserializeAsync<ApplicationState>(stream, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(SettingsDirectory);

        var tempPath = SettingsFilePath + ".tmp";

        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await _jsonSerializer.SerializeAsync(stream, CurrentSettings, cancellationToken);
        }

        File.Move(tempPath, SettingsFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public void SaveState()
    {
        Directory.CreateDirectory(SettingsDirectory);

        var tempPath = StateFilePath + ".tmp";
        var json = _jsonSerializer.Serialize(CurrentState);

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, StateFilePath, overwrite: true);
    }
}
