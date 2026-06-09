using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SlnDependencyStudio.Shared.Logging;

/// <summary>Host builder extensions for configuring Studio-wide Serilog logging.</summary>
public static class StudioHostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog with a rolling-file sink shared by all frontends.
    /// </summary>
    /// <remarks>
    /// <para>The log file base name and directory are resolved from <see cref="IConfiguration"/>:
    /// the config file path is read from <c>configuration["configFile"]</c> (or <c>configuration["cf"]</c>
    /// for the short-form CLI alias). When found, the log file is named
    /// <c>{configFileBaseName}-{Date}.txt</c> and written to a <c>logs</c> subfolder relative to the
    /// config file directory. When no config file path is available, the
    /// log file defaults to <c>studio-{Date}.txt</c> under <see cref="AppContext.BaseDirectory"/>.</para>
    /// </remarks>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">An optional callback that receives the <see cref="IServiceProvider"/>
    /// and the <see cref="LoggerConfiguration"/> so the caller can add frontend-specific sinks,
    /// enrichers, or filters (for example, a console sink for CLI, or a circular-buffer sink for WPF).</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseStudioSerilog(this IHostBuilder hostBuilder,
        Action<IServiceProvider, LoggerConfiguration>? configure = null)
    {
        return hostBuilder.UseSerilog((hostContext, services, configuration) =>
        {
            configuration.MinimumLevel.Information();

            configure?.Invoke(services, configuration);

            var (configFileBaseName, logDirectory) = ResolveLogNaming(hostContext.Configuration);

            var rollingFileName = Path.Combine(logDirectory, $"{configFileBaseName}-{{Date}}.txt");

            configuration.WriteTo.RollingFile(rollingFileName, retainedFileCountLimit: 31);
        });
    }

    private static (string BaseName, string LogDirectory) ResolveLogNaming(IConfiguration configuration)
    {
        var configFile = configuration["configFile"] ?? configuration["cf"];

        if (!string.IsNullOrEmpty(configFile))
        {
            var fullPath = Path.GetFullPath(configFile);
            var baseName = Path.GetFileNameWithoutExtension(fullPath);
            var logDir = Path.Combine(Path.GetDirectoryName(fullPath)!, "logs");

            return (baseName, logDir);
        }

        // When the WPF frontend is implemented, it will provide the log directory from
        // application settings rather than AppContext.BaseDirectory. This method will
        // need refactoring at that point to accept a frontend-specific log directory.
        var resolvedLogDir = Path.Combine(AppContext.BaseDirectory, "logs");

        return ("studio", resolvedLogDir);
    }
}