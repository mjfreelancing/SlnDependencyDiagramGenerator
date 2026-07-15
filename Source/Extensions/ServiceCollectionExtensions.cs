using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
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
            // Registration rules (keep these in sync with architecture decisions):
            // 1) Default to concrete registration for internal single-implementation plumbing.
            // 2) Use interface registration only when a boundary is intentionally public/extensible,
            //    or when multiple implementations must be selected at runtime.
            // 3) An interface is also valid when unit tests need a reliable mock/fake seam and
            //    no simpler seam exists.
            // 4) Do not make implementation classes public just for DI convenience.
            // 5) Start internal-first; introduce a public interface later only when a real consumer needs it.
            // 6) Keep AddSlnDependencyGenerator() the canonical composition root for this library.
            // 7) Keep service lifetimes Scoped unless there is a proven reason to change.

            // Internal plumbing
            // - single implementation
            // - no public abstraction required
            // - does not participate in unit testing (but is integration tested)
            services.AddScoped<ProjectAssetReader>();
            services.AddScoped<ISolutionParser, SolutionParser>();

            // Public generator-facing boundaries (used by DependencyGenerator and candidate frontend consumers).
            services.AddScoped<IProjectDiscoveryService, ProjectDiscoveryService>();
            services.AddScoped<IToolDetectionService, ToolDetectionService>();
            services.AddScoped<IDependencyGenerator, DependencyGenerator>();

            // Shared progress reporter — singleton so generator and renderers push to the same channel.
            services.AddSingleton<IProgressReporter, ProgressReporter>();

            // Renderer contract supports multiple implementations (D2, Mermaid).
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