using MaterialDesignThemes.Wpf;
using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Features.Theming;

/// <summary>Default implementation of <see cref="IThemeService"/>.
/// Uses the canonical <see cref="PaletteHelper"/> pattern from the official MaterialDesignInXaml demo.</summary>
internal sealed class ThemeService : IThemeService
{
    /// <inheritdoc/>
    public void ApplyTheme(StudioTheme theme)
    {
        var paletteHelper = new PaletteHelper();
        var currentTheme = paletteHelper.GetTheme();

        currentTheme.SetBaseTheme(theme == StudioTheme.Light ? BaseTheme.Light : BaseTheme.Dark);

        paletteHelper.SetTheme(currentTheme);
    }
}
