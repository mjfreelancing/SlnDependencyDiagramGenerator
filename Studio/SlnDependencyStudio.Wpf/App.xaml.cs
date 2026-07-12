using AllOverIt.Serilog.Extensions;
using AllOverIt.Serilog.Sinks.CircularBuffer;
using AllOverIt.Serilog.Sinks.Observable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI.Builder;
using Serilog;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Wpf.Extensions;
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
        // CircularBufferSinkMessages: retains the last N log events for session history.
        var observableSink = new ObservableSink();
        var circularBufferMessages = new CircularBufferSinkMessages(capacity: 500);

        _host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IObservableSink>(observableSink);
                services.AddSingleton<ICircularBufferSinkMessages>(circularBufferMessages);

                // Dependency diagram generator services from the core library.
                var (_, validationRegistry) = services.AddSlnDependencyGenerator();

                // Shared Studio services.
                services.AddSlnDependencyStudio(validationRegistry);

                // WPF-specific services.
                services.AddWpfDependencies();
            })
            .UseStudioSerilog((serviceProvider, configuration) =>
            {
                configuration.WriteTo.Observable(observableSink);

                var bufferMessages = serviceProvider.GetRequiredService<ICircularBufferSinkMessages>();
                configuration.WriteTo.CircularBuffer(bufferMessages);
            }, logDirectory: DefaultLogDirectory)
            .Build();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        var bootstrapper = _host.Services.GetRequiredService<SlnDependencyWpfAppBootstrapper>();
        await bootstrapper.RunAsync();
    }
}
