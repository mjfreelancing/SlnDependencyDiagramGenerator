using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI.Builder;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Wpf.Extensions;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        // ReactiveUI v23 requires explicit builder initialization before reactive mixins are used.
        var reactiveUiBuilder = RxAppBuilder.CreateReactiveUIBuilder();
        reactiveUiBuilder.WithCoreServices();
        reactiveUiBuilder.WithWpf();
        reactiveUiBuilder.BuildApp();

        _host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                // Dependency diagram generator services from the core library.
                var (_, validationRegistry) = services.AddSlnDependencyGenerator();

                // Shared Studio services.
                services.AddSlnDependencyStudio(validationRegistry);

                // WPF-specific services.
                services.AddWpfDependencies();

                // MainWindow is a singleton (matches the AllOverIt ViewRegistryDemo pattern).
                services.AddSingleton<MainWindow>();
            })
            .UseStudioSerilog()
            .Build();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
