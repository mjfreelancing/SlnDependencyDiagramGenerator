using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.IO;
using System.Text.Json;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>Default implementation of <see cref="IApplicationSettingsService"/>.
/// Persists settings to <c>%AppData%/SlnDependencyStudio/settings.json</c> using
/// <see cref="System.Text.Json"/> with an atomic write strategy.</summary>
internal sealed class ApplicationSettingsService : IApplicationSettingsService, IStudioSingletonDependency
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlnDependencyStudio");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public ApplicationSettings CurrentSettings { get; private set; } = new();

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (!File.Exists(SettingsFilePath))
        {
            CurrentSettings = new ApplicationSettings();
            return;
        }

        await using var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        CurrentSettings = (await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream, SerializerOptions))!;
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        Directory.CreateDirectory(SettingsDirectory);

        var tempPath = SettingsFilePath + ".tmp";

        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, CurrentSettings, SerializerOptions);
        }

        File.Move(tempPath, SettingsFilePath, overwrite: true);
    }
}
