using AllOverIt.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SlnDependencyStudio.Shared.Extensions;

/// <summary>Host builder extensions for configuring Studio-wide Serilog logging.</summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Defines extension methods for <see cref="IHostBuilder"/>.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Configures Serilog with a rolling-file sink shared by all frontends.
        /// </summary>
        /// <remarks>
        /// <para>The log directory is resolved differently depending on the frontend:</para>
        /// <list type="bullet">
        ///   <item><description><b>CLI:</b> The log file is written to a <c>logs</c> subfolder next to the config file
        ///   being processed (read from <c>configuration["configFile"]</c> or <c>configuration["cf"]</c>),
        ///   named <c>{configFileBaseName}-{Date}.txt</c>. If no config file path is available, it falls back
        ///   to <c>studio-{Date}.txt</c> under <see cref="AppContext.BaseDirectory"/>.</description></item>
        ///   <item><description><b>WPF:</b> Passes <paramref name="logDirectory"/> explicitly because there is no
        ///   single config file — projects are opened/closed from arbitrary locations over a session. The WPF
        ///   frontend uses <c>%AppData%/SlnDependencyStudio/Logs</c> as the fixed log directory.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="configure">An optional callback that receives the <see cref="IServiceProvider"/>
        /// and the <see cref="LoggerConfiguration"/> so the caller can add frontend-specific sinks,
        /// enrichers, or filters (for example, a console sink for CLI, or a circular-buffer sink for WPF).</param>
        /// <param name="logDirectory">When specified (WPF), overrides the auto-resolved log directory.
        /// When not specified (CLI), the directory is resolved relative to the config file path.</param>
        /// <returns>The host builder for chaining.</returns>
        public IHostBuilder UseStudioSerilog(Action<IServiceProvider, LoggerConfiguration>? configure = null,
            string? logDirectory = null)
        {
            return hostBuilder.UseSerilog((hostContext, services, configuration) =>
            {
                configuration.MinimumLevel.Information();

                configure?.Invoke(services, configuration);

                string resolvedBaseName;
                string resolvedLogDirectory;

                if (logDirectory is not null)
                {
                    // WPF: log to a fixed AppData location
                    resolvedBaseName = "studio";
                    resolvedLogDirectory = logDirectory;
                }
                else
                {
                    // CLI: log relative to the config file being processed
                    (resolvedBaseName, resolvedLogDirectory) = ResolveLogNaming(hostContext.Configuration);
                }

                var rollingFileName = Path.Combine(resolvedLogDirectory, $"{resolvedBaseName}-{{Date}}.txt");

                configuration.WriteTo.RollingFile(rollingFileName, retainedFileCountLimit: 31);
            });
        }
    }

    private static (string LogBaseName, string LogDirectory) ResolveLogNaming(IConfiguration configuration)
    {
        var configFile = configuration["configFile"] ?? configuration["cf"];

        if (configFile.IsNotNullOrEmpty())
        {
            var fullPath = Path.GetFullPath(configFile);
            var baseName = Path.GetFileNameWithoutExtension(fullPath);
            var logDir = Path.Combine(Path.GetDirectoryName(fullPath)!, "logs");

            return (baseName, logDir);
        }

        var fullLogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

        return ("studio", fullLogDirectory);
    }
}