using AllOverIt.GenericHost;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Cli;
using SlnDependencyStudio.Shared.Extensions;

await GenericHost
    .CreateConsoleHostBuilder<App>(args)
    .ConfigureServices((context, services) =>
    {
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);
    })
    .UseStudioSerilog((_, configuration) =>
    {
        configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);
    })
    .RunConsoleAsync(options => options.SuppressStatusMessages = true);
