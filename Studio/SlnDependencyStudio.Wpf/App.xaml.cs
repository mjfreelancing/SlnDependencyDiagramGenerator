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
using System.Reactive;
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
    private readonly ILogger<App> _logger;

    public App()
    {
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

        _logger = _host.Services.GetRequiredService<ILogger<App>>();

        // ReactiveUI v23 requires explicit builder initialization before reactive mixins are used (the
        // mixins are first exercised during Application_Startup, after the host is built). WithExceptionHandler
        // installs the app-wide safety net for unhandled ReactiveUI exceptions: any command/observable
        // exception with no targeted ThrownExceptions handler is logged and surfaced via a fallback message
        // box instead of crashing the app. The error dialog service needs a registered View handler
        // (MainWindow) and is not guaranteed here, so a plain message box is the deliberate fallback for the
        // global handlers.
        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithCoreServices()
            .WithWpf()
            .WithExceptionHandler(Observer.Create<Exception>(exception =>
            {
                try
                {
                    _logger.LogError(exception, "An unhandled ReactiveUI exception occurred.");

                    Dispatcher.BeginInvoke(() => ShowUnexpectedError(exception.Message));
                }
                catch
                {
                    // Never let the fallback handler itself throw - it runs on the exception path.
                }
            }))
            .BuildApp();

        // Outermost WPF safety net for exceptions that escape everything else (event handlers, async void).
        // Marked handled so the process does not terminate.
        DispatcherUnhandledException += (_, args) =>
        {
            _logger.LogError(args.Exception, "An unhandled exception reached the dispatcher.");

            ShowUnexpectedError(args.Exception.Message);
            args.Handled = true;
        };
    }

    /// <summary>Fallback error surface for the global handlers - a plain message box, since the
    /// error dialog service requires a registered View handler (MainWindow) that is not guaranteed here.</summary>
    private static void ShowUnexpectedError(string message)
    {
        MessageBox.Show(message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void Application_Startup(object sender, StartupEventArgs startupArgs)
    {
        try
        {
            _logger.LogInformation("SlnDependencyStudio is starting");

            await _host.StartAsync();

            var bootstrapper = _host.Services.GetRequiredService<WpfAppBootstrapper>();
            await bootstrapper.RunAsync();

            _logger.LogDebug("SlnDependencyStudio bootstrapper initialised");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Application startup failed");

            // Startup failed, potentially before the main window was shown, so ShutdownMode.OnLastWindowClose
            // never fires (it only triggers when a window actually closes).
            ShowUnexpectedError($"The application failed to start.\n\n{exception.Message}");

            // Shut the app down explicitly with a non-zero exit code instead of leaving the dispatcher running with no UI.
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs exitArgs)
    {
        _logger.LogInformation("SlnDependencyStudio exiting");

        base.OnExit(exitArgs);

        // Stop hosted services and flush the Serilog sink. OnExit is synchronous (the process is exiting),
        // so the host's synchronous Dispose — which stops the host internally — is used rather than awaiting
        // StopAsync. Runs after base.OnExit so Exit handlers still have the host (and the logger) available.
        _host.Dispose();
    }
}
