using AllOverIt.GenericHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Cli;
using SlnDependencyStudio.Shared.Logging;

await GenericHost
    .CreateConsoleHostBuilder<App>(args)
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<ConfigLoader>();
        services.AddSlnDependencyGenerator();
    })
    .UseStudioSerilog((_, configuration) => configuration.WriteTo.Console())
    .RunConsoleAsync(options => options.SuppressStatusMessages = true);
