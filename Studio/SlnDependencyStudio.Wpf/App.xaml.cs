using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactiveUI.Builder;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.Logging;
using SlnDependencyStudio.Wpf.Extensions;
using SlnDependencyStudio.Wpf.Features.Application;
using System.IO;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

/// <summary>The WPF application entry point. Owns the dependency injection host and
/// coordinates application startup and shutdown.</summary>
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

        // Create the log buffer that captures events emitted before the output panel subscribes.
        // StudioLogBuffer: queues entries until the OutputPanelViewModel subscribes (during
        // main-window construction), then relays live events. Rooted for the app lifetime.
        var logBuffer = new StudioLogBuffer();

        _host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IStudioLogBuffer>(logBuffer);

                // Dependency diagram generator services from the core library.
                var (_, validationRegistry) = services.AddSlnDependencyDiagramGenerator();

                // Shared Studio services.
                services.AddSlnDependencyStudio(validationRegistry);

                // WPF-specific services.
                services.AddWpfDependencies();
            })
            .UseStudioSerilog(
                (_, configuration) =>
                {
                    configuration.WriteTo.Sink(new StudioLogSink(logBuffer));
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

            logger.LogInformation("SlnDependencyStudio bootstrapper initialised");
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
