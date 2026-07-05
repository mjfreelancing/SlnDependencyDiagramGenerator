using AllOverIt.ReactiveUI.Factories;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Theming;

namespace SlnDependencyStudio.Wpf;

internal sealed class SlnDependencyWpfAppBootstrapper
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IThemeService _themeService;
    private readonly IViewFactory _viewFactory;

    public SlnDependencyWpfAppBootstrapper(IApplicationSettingsService applicationSettingsService,
        IThemeService themeService, IViewFactory viewFactory)
    {
        _applicationSettingsService = applicationSettingsService;
        _themeService = themeService;
        _viewFactory = viewFactory;
    }

    public async Task RunAsync()
    {
        try
        {
            // Load durable application settings before showing the main window.
            await _applicationSettingsService.LoadAsync();

            // Apply the persisted theme preference.
            _themeService.ApplyTheme(_applicationSettingsService.CurrentSettings.Theme);

            var mainWindow = (MainWindow)_viewFactory.CreateViewFor<MainWindowViewModel>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // TODO: Decide what to do here
        }
    }
}