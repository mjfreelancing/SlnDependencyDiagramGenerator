using SlnDependencyStudio.Wpf.Features.Application.Models;
using System.IO;
using System.Text.Json;

namespace SlnDependencyStudio.Wpf.Features.Application;

/// <summary>
/// Reads a minimal subset of the persisted application settings before the host is built, so values
/// that must be applied at startup (such as log retention) can be read before the DI container is available.
/// Failures are non-fatal and fall back to defaults because logging is not yet initialised at this point.
/// </summary>
internal static class ApplicationSettingsStartupReader
{
    /// <summary>The subset of <see cref="ApplicationSettings"/> that must be known before the host is built.</summary>
    private sealed record RetentionSettings(int LogRetentionDays);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Reads the persisted <see cref="ApplicationSettings.LogRetentionDays"/> from the default
    /// settings file (<c>%AppData%/SlnDependencyStudio/settings.json</c>).</summary>
    /// <returns>The persisted retention period in days, or <see cref="ApplicationSettings.DefaultLogRetentionDays"/>
    /// when the file is missing, cannot be read, or contains an invalid value.</returns>
    internal static int ReadLogRetentionDays()
    {
        var settingsFile = Path.Combine(
            ApplicationSettingsService.GetDefaultSettingsDirectory(),
            "settings.json");

        return ReadLogRetentionDays(settingsFile);
    }

    /// <summary>Reads the persisted <see cref="ApplicationSettings.LogRetentionDays"/> from a specific settings file.</summary>
    /// <param name="settingsFilePath">The full path to the settings file to read.</param>
    /// <returns>The persisted retention period in days, or <see cref="ApplicationSettings.DefaultLogRetentionDays"/>
    /// when the file is missing, cannot be read, or contains an invalid value.</returns>
    internal static int ReadLogRetentionDays(string settingsFilePath)
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return ApplicationSettings.DefaultLogRetentionDays;
            }

            var json = File.ReadAllText(settingsFilePath);
            var settings = JsonSerializer.Deserialize<RetentionSettings>(json, SerializerOptions);

            return settings is not null && settings.LogRetentionDays > 0
                ? settings.LogRetentionDays
                : ApplicationSettings.DefaultLogRetentionDays;
        }
        catch (Exception)
        {
            // Logging is not yet initialised at startup, so failures fall back to the default silently.
            return ApplicationSettings.DefaultLogRetentionDays;
        }
    }
}
