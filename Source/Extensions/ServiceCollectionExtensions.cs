using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using SlnDependencyDiagramGenerator.Validators;

namespace SlnDependencyDiagramGenerator.Extensions;

/// <summary>
/// Contains the result of registering SlnDependencyDiagramGenerator services with DI. This is returned from
/// <see cref="ServiceCollectionExtensions.AddSlnDependencyDiagramGenerator"/> and contains both the service collection
/// and validation registry for chaining additional registrations if needed.
/// </summary>
/// <param name="Services">The service collection.</param>
/// <param name="ValidationRegistry">The registry used for registering model validators.</param>
public sealed record SlnDependencyDiagramGeneratorRegistration(IServiceCollection Services, IValidationRegistry ValidationRegistry);

/// <summary>Extension methods for registering SlnDependencyDiagramGenerator services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Defines extension methods for registering SlnDependencyDiagramGenerator services.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers all SlnDependencyDiagramGenerator services with the service collection.</summary>
        /// <returns>The registration containing the service collection and validation registry, for chaining.</returns>
        public SlnDependencyDiagramGeneratorRegistration AddSlnDependencyDiagramGenerator()
        {
            services.AddSingleton<ToolPathOverridesProvider>(() => []);
            services.AddScoped<IProjectAssetReader, ProjectAssetReader>();
            services.AddScoped<ISolutionProjectResolver, SlnSolutionProjectResolver>();
            services.AddScoped<ISolutionProjectResolver, SlnxSolutionProjectResolver>();
            services.AddScoped<ISolutionParser, SolutionParser>();
            services.AddScoped<IProjectDiscoveryService, ProjectDiscoveryService>();
            services.AddScoped<IToolDetectionService, ToolDetectionService>();
            services.AddScoped<IToolPathResolver, ToolPathResolver>();
            services.AddScoped<IDependencyGenerator, DependencyGenerator>();

            var validationRegistry = services.AddValidationInvoker(validationRegistry =>
            {
                validationRegistry.AutoRegisterValidators<ValidationRegistrar>();
            });

            return new SlnDependencyDiagramGeneratorRegistration(services, validationRegistry);
        }
    }
}