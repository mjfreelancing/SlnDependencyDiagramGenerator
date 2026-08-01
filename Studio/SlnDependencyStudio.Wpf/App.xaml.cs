using AllOverIt.Serilog.Extensions;
using AllOverIt.Serilog.Sinks.Observable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactiveUI.Builder;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Wpf.Extensions;
using SlnDependencyStudio.Wpf.Features.Application;
using System.IO;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

public partial class App : Application
{
    private static readonly string DefaultLogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlnDependencyStudio", "Logs");

    private readonly IHost _host;

    public App()
    {
        // ReactiveUI v23 requires explicit builder initialization before reactive mixins are used.
        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithCoreServices()
            .WithWpf()
            .BuildApp();

        // Create sink instances that must live for the application lifetime.
        // ObservableSink: streams log events to subscribers (OutputPanelViewModel).
        var observableSink = new ObservableSink();

        _host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IObservableSink>(observableSink);

                // Dependency diagram generator services from the core library.
                var (_, validationRegistry) = services.AddSlnDependencyGenerator();

                // Shared Studio services.
                services.AddSlnDependencyStudio(validationRegistry);

                // WPF-specific services.
                services.AddWpfDependencies();
            })
            .UseStudioSerilog(
                (_, configuration) =>
                {
                    configuration.WriteTo.Observable(observableSink);
                },
                logDirectory: DefaultLogDirectory,
                retentionDays: ApplicationSettingsStartupReader.ReadLogRetentionDays())
            .Build();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();

        try
        {
            logger.LogInformation("SlnDependencyStudio is starting");

            await _host.StartAsync();

            var bootstrapper = _host.Services.GetRequiredService<SlnDependencyWpfAppBootstrapper>();
            await bootstrapper.RunAsync();

            logger.LogInformation("SlnDependencyStudio startup initialised");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Application startup failed");

            throw;
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("SlnDependencyStudio exiting");

        base.OnExit(e);
    }
}
