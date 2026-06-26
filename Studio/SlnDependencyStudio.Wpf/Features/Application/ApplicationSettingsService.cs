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

    private readonly IStudioJsonSerializer _jsonSerializer;

    /// <inheritdoc />
    public ApplicationSettings CurrentSettings { get; private set; } = new();

    /// <summary>Initializes a new instance of <see cref="ApplicationSettingsService"/>.</summary>
    /// <param name="jsonSerializer">The JSON serializer used to persist and load settings.</param>
    public ApplicationSettingsService(IStudioJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer.WhenNotNull();
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SettingsFilePath))
        {
            CurrentSettings = new ApplicationSettings();
            return;
        }

        await using var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        CurrentSettings = (await _jsonSerializer.DeserializeAsync<ApplicationSettings>(stream, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(SettingsDirectory);

        var tempPath = SettingsFilePath + ".tmp";

        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await _jsonSerializer.SerializeAsync(stream, CurrentSettings, cancellationToken);
        }

        File.Move(tempPath, SettingsFilePath, overwrite: true);
    }
}
