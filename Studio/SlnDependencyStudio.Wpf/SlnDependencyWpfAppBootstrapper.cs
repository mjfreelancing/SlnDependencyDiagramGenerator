using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Theming;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

internal sealed class SlnDependencyWpfAppBootstrapper
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IThemeService _themeService;
    private readonly IViewFactory _viewFactory;
    private readonly ILogger<SlnDependencyWpfAppBootstrapper> _logger;

    public SlnDependencyWpfAppBootstrapper(IApplicationSettingsService applicationSettingsService,
        IThemeService themeService, IViewFactory viewFactory, ILogger<SlnDependencyWpfAppBootstrapper> logger)
    {
        _applicationSettingsService = applicationSettingsService;
        _themeService = themeService;
        _viewFactory = viewFactory;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        try
        {
            _logger.LogInformation("Loading application settings");

            // Load durable application settings before showing the main window.
            await _applicationSettingsService.LoadAsync();

            var settings = _applicationSettingsService.CurrentSettings;

            _logger.LogInformation("Application settings loaded");

            _logger.LogDebug("Settings: DefaultProjectFolder={Folder}, LogRetentionDays={RetentionDays}, Theme={Theme}",
                settings.DefaultProjectFolder, settings.LogRetentionDays, settings.Theme);

            // Apply the persisted theme preference.
            _themeService.ApplyTheme(settings.Theme);

            _logger.LogDebug("Creating main view");

            var mainWindow = (MainWindow)_viewFactory.CreateViewFor<MainWindowViewModel>();

            _logger.LogDebug("Showing Main window");

            mainWindow.Show();

            _logger.LogDebug("Main window shown");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to start the application shell");

            MessageBox.Show(exception.Message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}