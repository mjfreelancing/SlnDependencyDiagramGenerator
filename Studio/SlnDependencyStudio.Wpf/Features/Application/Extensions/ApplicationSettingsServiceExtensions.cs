using AllOverIt.Extensions;
using System.IO;

namespace SlnDependencyStudio.Wpf.Features.Application.Extensions;

/// <summary>Extension methods for <see cref="IApplicationSettingsService"/>.</summary>
public static class ApplicationSettingsServiceExtensions
{
    /// <summary>
    /// Defines extension methods for <see cref="IApplicationSettingsService"/>.
    /// </summary>
    /// <param name="service">The application settings service.</param>
    extension(IApplicationSettingsService service)
    {
        /// <summary>Returns the configured default project folder if it is set and exists on disk;
        /// otherwise returns <see cref="string.Empty"/> so file dialogs fall back to the system default.</summary>
        /// <returns>The configured folder path, or an empty string.</returns>
        public string ResolveProjectFolder()
        {
            var folder = service.CurrentSettings.DefaultProjectFolder;

            return folder.IsNotNullOrEmpty() && Directory.Exists(folder)
                ? folder
                : string.Empty;
        }
    }
}
