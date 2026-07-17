using AllOverIt.DependencyInjection.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Validators;

namespace SlnDependencyStudio.Shared.Extensions;

/// <summary>Extension methods for registering Studio shared services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Defines extension methods for registering Studio shared services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers all shared Sln Dependency Studio services with the service collection.</summary>
        /// <param name="validationRegistry">The validation registry to register additional validators with. This source registry
        /// is expected to have been returned from <see cref="SlnDependencyDiagramGenerator.Extensions.ServiceCollectionExtensions.AddSlnDependencyGenerator"/>.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddSlnDependencyStudio(IValidationRegistry validationRegistry)
        {
            // Auto-register all classes implementing marker interfaces found in this assembly.
            services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
            {
                config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency));
            });

            // Auto-register all validators found in this assembly.
            validationRegistry.AutoRegisterValidators<ValidationRegistrar>();

            return services;
        }
    }
}