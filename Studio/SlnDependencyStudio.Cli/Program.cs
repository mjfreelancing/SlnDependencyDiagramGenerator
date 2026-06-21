using AllOverIt.DependencyInjection.Extensions;
using AllOverIt.GenericHost;
using Microsoft.Extensions.Hosting;
using Serilog;
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
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();

        // Shared Studio services
        services.AddSlnDependencyStudio(validationRegistry);

        // CLI-specific services
        // Auto-register all classes implementing marker interfaces found in this assembly.
        services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
        {
            config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency));
        });
    })
    .UseStudioSerilog((_, configuration) =>
    {
        configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);
    })
    .RunConsoleAsync(options => options.SuppressStatusMessages = true);
