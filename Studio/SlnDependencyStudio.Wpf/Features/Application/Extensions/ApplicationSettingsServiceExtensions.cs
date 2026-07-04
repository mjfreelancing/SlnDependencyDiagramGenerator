using System.IO;

namespace SlnDependencyStudio.Wpf.Features.Application.Extensions;

/// <summary>Extension methods for <see cref="IApplicationSettingsService"/>.</summary>
public static class ApplicationSettingsServiceExtensions
{
    /// <summary>Returns the configured default project folder if it is set and exists on disk;
    /// otherwise returns <see cref="string.Empty"/> so file dialogs fall back to the system default.</summary>
    /// <param name="service">The application settings service.</param>
    /// <returns>The configured folder path, or an empty string.</returns>
    public static string ResolveProjectFolder(this IApplicationSettingsService service)
    {
        var folder = service.CurrentSettings.DefaultProjectFolder;

        return !string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder)
            ? folder
            : string.Empty;
    }
}
