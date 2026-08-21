using AllOverIt.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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
        ///   <item><description><b>CLI:</b> The log file is written to a <c>logs</c> subfolder next to the project file
        ///   being processed (read from <c>configuration["projectFile"]</c> or <c>configuration["pf"]</c>),
        ///   named <c>{projectFileBaseName}-yyyyMMdd.txt</c>. If no project file path is available, it falls back
        ///   to <c>studio-yyyyMMdd.txt</c> under <see cref="AppContext.BaseDirectory"/>.</description></item>
        ///   <item><description><b>WPF:</b> Passes <paramref name="logDirectory"/> explicitly because there is no
        ///   single project file — projects are opened/closed from arbitrary locations over a session. The WPF
        ///   frontend uses <c>%AppData%/SlnDependencyStudio/Logs</c> as the fixed log directory.</description></item>
        /// </list>
        /// <para>The rolling file sink always writes at <see cref="LogEventLevel.Debug"/>. A
        /// <see cref="LoggingLevelSwitch"/> is registered so a frontend can attach an interactive sink
        /// (for example, the CLI console) whose verbosity it controls independently of the file.</para>
        /// </remarks>
        /// <param name="configure">An optional callback that receives the <see cref="IServiceProvider"/>
        /// and the <see cref="LoggerConfiguration"/> so the caller can add frontend-specific sinks,
        /// enrichers, or filters (for example, a console sink for CLI, or the WPF log-buffer sink).</param>
        /// <param name="logDirectory">When specified (WPF), overrides the auto-resolved log directory.
        /// When not specified (CLI), the directory is resolved relative to the project file path.</param>
        /// <param name="retentionDays">The number of days of rolling log files to retain (one file is written
        /// per day). When not specified, Serilog's default retention applies.</param>
        /// <returns>The host builder for chaining.</returns>
        public IHostBuilder UseStudioSerilog(Action<IServiceProvider, LoggerConfiguration>? configure = null,
            string? logDirectory = null, int? retentionDays = null)
        {
            // Registered for frontend use (for example, the CLI's --verbose toggle). The file sink
            // is not controlled by this switch — it always writes at Debug.
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            hostBuilder.ConfigureServices((_, services) =>
            {
                services.AddSingleton(levelSwitch);
            });

            return hostBuilder.UseSerilog((hostContext, services, configuration) =>
            {
                // The file sink always captures at Debug. Frontends may attach interactive sinks
                // (for example, the CLI console) to the registered LoggingLevelSwitch to control
                // their own verbosity independently of the file.
                configuration.MinimumLevel.Debug();

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
                    // CLI: log relative to the project file being processed
                    (resolvedBaseName, resolvedLogDirectory) = ResolveLogNaming(hostContext.Configuration);
                }

                var rollingFileName = Path.Combine(resolvedLogDirectory, $"{resolvedBaseName}-.txt");

                // Daily rolling writes one file per day, so Serilog's retainedFileCountLimit effectively retains
                // the most recent `retentionDays` days of logs (Serilog's default is 31).
                configuration.WriteTo.File(rollingFileName, rollingInterval: RollingInterval.Day, retainedFileCountLimit: retentionDays);
            });
        }
    }

    private static (string LogBaseName, string LogDirectory) ResolveLogNaming(IConfiguration configuration)
    {
        var projectFile = configuration["projectFile"] ?? configuration["pf"];

        if (projectFile.IsNotNullOrEmpty())
        {
            var fullPath = Path.GetFullPath(projectFile);
            var baseName = Path.GetFileNameWithoutExtension(fullPath);
            var logDir = Path.Combine(Path.GetDirectoryName(fullPath)!, "logs");

            return (baseName, logDir);
        }

        var fullLogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

        return ("studio", fullLogDirectory);
    }
}