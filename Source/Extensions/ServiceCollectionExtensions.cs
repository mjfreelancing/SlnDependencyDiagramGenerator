using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using SlnDependencyDiagramGenerator.Renderers;
using SlnDependencyDiagramGenerator.Renderers.D2;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;
using SlnDependencyDiagramGenerator.Validators;

namespace SlnDependencyDiagramGenerator.Extensions;

/// <summary>
/// Contains the result of registering SlnDependencyDiagramGenerator services with DI. This is returned from
/// <see cref="ServiceCollectionExtensions.AddSlnDependencyGenerator"/> and contains both the service collection
/// and validation registry for chaining additional registrations if needed.
/// </summary>
/// <<param name="Services">The service collection.</param>
/// <param name="ValidationRegistry">The registry used for registering model validators.</param>>
public sealed record SlnDependencyDiagramGeneratorRegistration(IServiceCollection Services, IValidationRegistry ValidationRegistry);

/// <summary>Extension methods for registering SlnDependencyDiagramGenerator services with DI.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers all SlnDependencyDiagramGenerator services with the service collection.</summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection, for chaining.</returns>
        public SlnDependencyDiagramGeneratorRegistration AddSlnDependencyGenerator()
        {
            services.AddScoped<IProjectAssetReader, ProjectAssetReader>();
            services.AddScoped<ISolutionProjectResolver, SlnSolutionProjectResolver>();
            services.AddScoped<ISolutionProjectResolver, SlnxSolutionProjectResolver>();
            services.AddScoped<ISolutionParser, SolutionParser>();
            services.AddScoped<IProjectDiscoveryService, ProjectDiscoveryService>();
            services.AddScoped<IToolDetectionService, ToolDetectionService>();
            services.AddSingleton<ToolPathOverridesProvider>(() => []);
            services.AddScoped<IToolPathResolver, ToolPathResolver>();
            services.AddScoped<IDependencyGenerator, DependencyGenerator>();
            services.AddSingleton<IProgressReporter, ProgressReporter>();
            services.AddScoped<IDiagramRenderer, D2DiagramRenderer>();
            services.AddScoped<IDiagramRenderer, MermaidDiagramRenderer>();

            var validationRegistry = services.AddValidationInvoker(validationRegistry =>
            {
                validationRegistry.AutoRegisterValidators<ValidationRegistrar>();
            });

            return new SlnDependencyDiagramGeneratorRegistration(services, validationRegistry);
        }
    }
}