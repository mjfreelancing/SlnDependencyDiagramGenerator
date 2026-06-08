using AllOverIt.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SlnDependencyStudio.Shared.Logging;

/// <summary>Host builder extensions for configuring Studio-wide Serilog logging.</summary>
public static class StudioHostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog with a rolling-file sink shared by all frontends.
    /// Log files are written to a <c>logs</c> subfolder under the application directory,
    /// named after the project configuration file (without extension) so CLI and WPF
    /// processing the same project share the same log.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">An optional callback that receives the <see cref="IServiceProvider"/>
    /// and the <see cref="LoggerConfiguration"/> so the caller can add frontend-specific sinks,
    /// enrichers, or filters (for example, a console sink for CLI, or a circular-buffer sink for WPF).</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseStudioSerilog(this IHostBuilder hostBuilder, Action<IServiceProvider, LoggerConfiguration>? configure = null)
    {
        return hostBuilder.UseSerilog((hostContext, services, configuration) =>
        {
            configuration.MinimumLevel.Information();

            configure?.Invoke(services, configuration);

            var configFileBaseName = GetConfigFileBaseName(hostContext.Configuration);
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            var rollingFileName = Path.Combine(logDirectory, $"{configFileBaseName}-{{Date}}.txt");

            configuration.WriteTo.RollingFile(rollingFileName, retainedFileCountLimit: 31);
        });
    }

    private static string GetConfigFileBaseName(IConfiguration configuration)
    {
        var configFile = configuration["configFile"];

        if (configFile.IsNullOrEmpty())
        {
            return "studio";
        }

        return Path.GetFileNameWithoutExtension(configFile);
    }
}