using AllOverIt.ReactiveUI.Factories;
using SlnDependencyStudio.Wpf.Features.Application;

namespace SlnDependencyStudio.Wpf;

internal sealed class SlnDependencyWpfAppBootstrapper
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IViewFactory _viewFactory;

    public SlnDependencyWpfAppBootstrapper(IApplicationSettingsService applicationSettingsService, IViewFactory viewFactory)
    {
        _applicationSettingsService = applicationSettingsService;
        _viewFactory = viewFactory;
    }

    public async Task RunAsync()
    {
        try
        {
            // Load durable application settings before showing the main window.
            await _applicationSettingsService.LoadAsync();

            var mainWindow = (MainWindow)_viewFactory.CreateViewFor<MainWindowViewModel>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // TODO: Decide what to do here
        }
    }
}