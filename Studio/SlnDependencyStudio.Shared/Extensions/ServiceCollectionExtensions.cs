using AllOverIt.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Shared.Extensions;

/// <summary>Extension methods for registering Studio shared services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all shared Sln Dependency Studio services with the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSlnDependency(this IServiceCollection services)
    {
        // Auto-register all classes implementing marker interfaces found in this assembly.
        services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(
            config => config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency)));

        return services;
    }
}