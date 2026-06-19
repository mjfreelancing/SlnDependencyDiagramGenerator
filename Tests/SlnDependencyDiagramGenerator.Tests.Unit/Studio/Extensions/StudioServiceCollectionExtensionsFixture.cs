using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.Services;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Studio.Extensions;

public class StudioServiceCollectionExtensionsFixture
{
    [Fact]
    public void Should_Resolve_IStudioGenerationService_After_AddStudioServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DependencyGenerator());
        services.AddSlnDependency();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var generationService = scope.ServiceProvider.GetService<IStudioGenerationService>();

        generationService.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Not_Register_Marker_Interface_As_Service()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DependencyGenerator());
        services.AddSlnDependency();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var marker = scope.ServiceProvider.GetService<IStudioScopedDependency>();

        marker.ShouldBeNull();
    }
}