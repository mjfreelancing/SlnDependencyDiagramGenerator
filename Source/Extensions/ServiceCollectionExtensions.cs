using AllOverIt.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Renderers;
using SlnDependencyDiagramGenerator.Renderers.D2;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;
using SlnDependencyDiagramGenerator.Validators;

namespace SlnDependencyDiagramGenerator.Extensions;

/// <summary>Extension methods for registering SlnDependencyDiagramGenerator services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all SlnDependencyDiagramGenerator services with the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSlnDependencyGenerator(this IServiceCollection services)
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
        // 7) Use TryAdd* to allow host-level overrides without duplicate registrations.
        // 8) Keep service lifetimes Scoped unless there is a proven reason to change.

        // Internal plumbing
        // - single implementation
        // - no public abstraction required
        // - does not participate in unit testing (but is integration tested)
        services.TryAddScoped<ProjectAssetReader>();
        services.TryAddScoped<SolutionParser>();

        // Public generator-facing boundaries (used by DependencyGenerator and candidate frontend consumers).
        services.TryAddScoped<IProjectDiscoveryService, ProjectDiscoveryService>();
        services.TryAddScoped<IToolDetectionService, ToolDetectionService>();
        services.TryAddScoped<DependencyGenerator>();

        // Renderer contract supports multiple implementations (D2, Mermaid).
        services.TryAddScoped<IDiagramRenderer, D2DiagramRenderer>();
        services.TryAddScoped<IDiagramRenderer, MermaidDiagramRenderer>();

        services.AddValidationInvoker(validationRegistry =>
        {
            validationRegistry.AutoRegisterValidators<ValidationRegistrar>();
        });

        return services;
    }
}