using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Theming;
using System.Windows;

namespace SlnDependencyStudio.Wpf;

/// <summary>Bootstraps the application after the DI host has started: loads application settings,
/// applies the persisted theme, and shows the main window.</summary>
internal sealed class WpfAppBootstrapper
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IThemeService _themeService;
    private readonly IViewFactory _viewFactory;
    private readonly ILogger<WpfAppBootstrapper> _logger;

    /// <summary>Initializes a new instance of <see cref="WpfAppBootstrapper"/>.</summary>
    /// <param name="applicationSettingsService">The application settings service.</param>
    /// <param name="themeService">The theme service used to apply the persisted theme.</param>
    /// <param name="viewFactory">The view factory used to create the main window.</param>
    /// <param name="logger">The logger instance.</param>
    public WpfAppBootstrapper(IApplicationSettingsService applicationSettingsService,
        IThemeService themeService, IViewFactory viewFactory, ILogger<WpfAppBootstrapper> logger)
    {
        _applicationSettingsService = applicationSettingsService;
        _themeService = themeService;
        _viewFactory = viewFactory;
        _logger = logger;
    }

    /// <summary>Loads application settings, applies the persisted theme, and shows the main window.
    /// On failure, logs the error and displays a message box.</summary>
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