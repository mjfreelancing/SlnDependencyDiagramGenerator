using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

internal sealed class WpfHostLifetime : IHostLifetime
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<WpfHostLifetime> _logger;

    public WpfHostLifetime(IHostApplicationLifetime applicationLifetime, ILogger<WpfHostLifetime> logger)
    {
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Host lifetime starting");

        // When WPF exits gracefully, notify the generic host to shut down
        Application.Current?.Exit += OnWpfExit;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Host lifetime stopping");

        // Clean up event handler
        Application.Current?.Exit -= OnWpfExit;

        return Task.CompletedTask;
    }

    private void OnWpfExit(object sender, ExitEventArgs e)
    {
        _logger.LogDebug("Host lifetime exiting");

        // Signal the application lifetime so the host shuts down when the UI closes.
        _applicationLifetime.StopApplication();
    }
}
