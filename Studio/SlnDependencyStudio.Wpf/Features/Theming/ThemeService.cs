using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Features.Theming;

/// <summary>Default implementation of <see cref="IThemeService"/>.
/// Uses the canonical <see cref="PaletteHelper"/> pattern from the official MaterialDesignInXaml demo.</summary>
internal sealed class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;

    /// <summary>Initializes a new instance of <see cref="ThemeService"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void ApplyTheme(StudioTheme theme)
    {
        _logger.LogDebug("Theme applied: {Theme}", theme);

        var paletteHelper = new PaletteHelper();
        var currentTheme = paletteHelper.GetTheme();

        currentTheme.SetBaseTheme(theme == StudioTheme.Light ? BaseTheme.Light : BaseTheme.Dark);

        paletteHelper.SetTheme(currentTheme);
    }
}
