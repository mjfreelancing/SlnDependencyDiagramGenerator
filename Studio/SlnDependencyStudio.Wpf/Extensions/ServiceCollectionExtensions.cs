using AllOverIt.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Extensions;

/// <summary>Extension methods for registering WPF-specific Studio services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers WPF-specific services with the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWpfDependencies(this IServiceCollection services)
    {
        // Auto-register all scoped classes implementing IStudioScopedDependency in this assembly.
        services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
        {
            config.Filter((serviceType, _) => serviceType != typeof(IStudioScopedDependency));
        });

        // AutoRegisterTransient is deferred until needed by a specific phase.

        // Auto-register all singleton classes implementing IStudioSingletonDependency in this assembly.
        services.AutoRegisterSingleton<DependencyRegistrar, IStudioSingletonDependency>(config =>
        {
            config.Filter((serviceType, _) => serviceType != typeof(IStudioSingletonDependency));
        });

        return services;
    }
}
