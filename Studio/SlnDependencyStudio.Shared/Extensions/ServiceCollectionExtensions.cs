using AllOverIt.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace SlnDependencyStudio.Shared.DependencyInjection;

/// <summary>Extension methods for registering Studio shared services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all shared Studio services with the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddStudioServices(this IServiceCollection services)
    {
        // Auto-register all classes implementing marker interfaces found in this assembly.
        services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(
            config => config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency)));

        return services;
    }
}