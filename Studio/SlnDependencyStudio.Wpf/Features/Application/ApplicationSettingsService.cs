using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ApplicationSettingsService> _logger;
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
    /// <param name="logger">The logger instance.</param>
    public ApplicationSettingsService(IStudioJsonSerializer jsonSerializer, IFileSystem fileSystem,
        ILogger<ApplicationSettingsService> logger)
        : this(jsonSerializer, fileSystem, GetDefaultSettingsDirectory(), logger)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ApplicationSettingsService"/>
    /// with a custom settings directory (used by tests).</summary>
    /// <param name="jsonSerializer">The JSON serializer used to persist and load settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="settingsDirectory">The directory to store settings and state files.</param>
    /// <param name="logger">The logger instance.</param>
    internal ApplicationSettingsService(IStudioJsonSerializer jsonSerializer, IFileSystem fileSystem,
        string settingsDirectory, ILogger<ApplicationSettingsService> logger)
    {
        _jsonSerializer = jsonSerializer;
        _fileSystem = fileSystem;
        _logger = logger;
        _settingsDirectory = settingsDirectory;
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

        _logger.LogDebug("Loading settings from {SettingsFilePath}", _settingsFilePath);

        try
        {
            await using var stream = _fileSystem.OpenRead(_settingsFilePath);
            CurrentSettings = (await _jsonSerializer.DeserializeAsync<ApplicationSettings>(stream, cancellationToken))!;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load settings from {SettingsFilePath}; falling back to defaults: {ErrorMessage}", _settingsFilePath, exception.Message);
            CurrentSettings = new ApplicationSettings();
        }
    }

    private async Task LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(_stateFilePath))
        {
            CurrentState = new ApplicationState();
            return;
        }

        _logger.LogDebug("Loading application state from {StateFilePath}", _stateFilePath);

        try
        {
            await using var stream = _fileSystem.OpenRead(_stateFilePath);
            CurrentState = (await _jsonSerializer.DeserializeAsync<ApplicationState>(stream, cancellationToken))!;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Failed to load application state from {StateFilePath}; falling back to defaults: {ErrorMessage}", _stateFilePath, exception.Message);
            CurrentState = new ApplicationState();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving settings to {SettingsFilePath}", _settingsFilePath);

        try
        {
            _fileSystem.CreateDirectory(_settingsDirectory);

            var tempPath = _settingsFilePath + ".tmp";

            await using (var stream = _fileSystem.OpenWrite(tempPath))
            {
                await _jsonSerializer.SerializeAsync(stream, CurrentSettings, cancellationToken);
            }

            _fileSystem.MoveFile(tempPath, _settingsFilePath, overwrite: true);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to save settings to {SettingsFilePath}: {ErrorMessage}", _settingsFilePath, exception.Message);

            throw;
        }
    }

    /// <inheritdoc />
    public void SaveState()
    {
        _logger.LogDebug("Saving application state to {StateFilePath}", _stateFilePath);

        try
        {
            _fileSystem.CreateDirectory(_settingsDirectory);

            var tempPath = _stateFilePath + ".tmp";
            var json = _jsonSerializer.Serialize(CurrentState);

            _fileSystem.WriteAllText(tempPath, json);
            _fileSystem.MoveFile(tempPath, _stateFilePath, overwrite: true);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to save application state to {StateFilePath}: {ErrorMessage}", _stateFilePath, exception.Message);
            throw;
        }
    }

    /// <summary>Returns the default directory used to persist settings and state
    /// (<c>%AppData%/SlnDependencyStudio</c>).</summary>
    /// <returns>The default settings directory path.</returns>
    internal static string GetDefaultSettingsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlnDependencyStudio");
    }
}
