using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Features.Theming;

/// <summary>Service for applying the application theme (Light/Dark) at runtime.</summary>
public interface IThemeService : IStudioSingletonDependency
{
    /// <summary>Applies the specified theme to the application.</summary>
    /// <param name="theme">The theme to apply.</param>
    void ApplyTheme(StudioTheme theme);
}
