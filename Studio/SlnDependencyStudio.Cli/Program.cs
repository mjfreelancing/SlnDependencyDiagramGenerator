using AllOverIt.DependencyInjection.Extensions;
using AllOverIt.GenericHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Cli;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Extensions;

await GenericHost
    .CreateConsoleHostBuilder<App>(args)
    .ConfigureServices((context, services) =>
    {
        // Dependency diagram generator services
        var (_, validationRegistry) = services.AddSlnDependencyDiagramGenerator();

        // Shared Studio services
        services.AddSlnDependencyStudio(validationRegistry);

        // CLI-specific services
        // Auto-register all classes implementing marker interfaces found in this assembly.
        services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
        {
            config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency));
        });
    })
    .UseStudioSerilog((services, configuration) =>
    {
        // The console is an interactive sink whose verbosity is controlled by the --verbose flag
        // (via the registered LoggingLevelSwitch), independent of the always-Debug file sink.
        var levelSwitch = services.GetRequiredService<LoggingLevelSwitch>();

        configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code, levelSwitch: levelSwitch);
    })
    .RunConsoleAsync(options => options.SuppressStatusMessages = true);
