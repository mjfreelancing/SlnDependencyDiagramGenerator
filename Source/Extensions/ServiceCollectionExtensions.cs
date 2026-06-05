using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Renderers;
using SlnDependencyDiagramGenerator.Renderers.D2;
using SlnDependencyDiagramGenerator.Renderers.Mermaid;

namespace SlnDependencyDiagramGenerator.Extensions;

/// <summary>Extension methods for registering SlnDependencyDiagramGenerator services with DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all SlnDependencyDiagramGenerator services with the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSlnDependencyGenerator(this IServiceCollection services)
    {
        services.TryAddScoped<IProjectDiscoveryService, ProjectDiscoveryService>();
        services.TryAddScoped<IDiagramRenderer, D2DiagramRenderer>();
        services.TryAddScoped<IDiagramRenderer, MermaidDiagramRenderer>();

        return services;
    }
}