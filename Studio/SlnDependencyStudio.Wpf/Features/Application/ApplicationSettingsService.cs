using AllOverIt.Assertion;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>Default implementation of <see cref="IApplicationSettingsService"/>.
/// Persists settings to a configurable directory (defaults to <c>%AppData%/SlnDependencyStudio</c>).</summary>
internal sealed class ApplicationSettingsService : IApplicationSettingsService
{
    private readonly IStudioJsonSerializer _jsonSerializer;
    private readonly IFileSystem _fileSystem;
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly string _stateFilePath;

    /// <inheritdoc />
    public ApplicationSettings CurrentSettings { get; private set; } = new();

    /// <inheritdoc />
    public ApplicationState CurrentState { get; private set; } = new();

    /// <summary>Initializes a new instance of <see cref="ApplicationSettingsService"/>
    /// with the default <c>%AppData%/SlnDependencyStudio</c> directory.</summary>
    /// <param name="jsonSerializer">The JSON serializer used to persist and load settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    public ApplicationSettingsService(IStudioJsonSerializer jsonSerializer, IFileSystem fileSystem)
        : this(jsonSerializer, fileSystem, GetDefaultSettingsDirectory())
    {
    }

    /// <summary>Initializes a new instance of <see cref="ApplicationSettingsService"/>
    /// with a custom settings directory (used by tests).</summary>
    /// <param name="jsonSerializer">The JSON serializer used to persist and load settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="settingsDirectory">The directory to store settings and state files.</param>
    internal ApplicationSettingsService(IStudioJsonSerializer jsonSerializer, IFileSystem fileSystem, string settingsDirectory)
    {
        _jsonSerializer = jsonSerializer.WhenNotNull();
        _fileSystem = fileSystem.WhenNotNull();
        _settingsDirectory = settingsDirectory.WhenNotNull();
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        _stateFilePath = Path.Combine(_settingsDirectory, "state.json");
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await LoadSettingsAsync(cancellationToken);
        await LoadStateAsync(cancellationToken);
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(_settingsFilePath))
        {
            CurrentSettings = new ApplicationSettings();
            return;
        }

        await using var stream = _fileSystem.OpenRead(_settingsFilePath);
        CurrentSettings = (await _jsonSerializer.DeserializeAsync<ApplicationSettings>(stream, cancellationToken))!;
    }

    private async Task LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(_stateFilePath))
        {
            CurrentState = new ApplicationState();
            return;
        }

        await using var stream = _fileSystem.OpenRead(_stateFilePath);
        CurrentState = (await _jsonSerializer.DeserializeAsync<ApplicationState>(stream, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        _fileSystem.CreateDirectory(_settingsDirectory);

        var tempPath = _settingsFilePath + ".tmp";

        await using (var stream = _fileSystem.OpenWrite(tempPath))
        {
            await _jsonSerializer.SerializeAsync(stream, CurrentSettings, cancellationToken);
        }

        _fileSystem.MoveFile(tempPath, _settingsFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public void SaveState()
    {
        _fileSystem.CreateDirectory(_settingsDirectory);

        var tempPath = _stateFilePath + ".tmp";
        var json = _jsonSerializer.Serialize(CurrentState);

        _fileSystem.WriteAllText(tempPath, json);
        _fileSystem.MoveFile(tempPath, _stateFilePath, overwrite: true);
    }

    private static string GetDefaultSettingsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlnDependencyStudio");
    }
}
