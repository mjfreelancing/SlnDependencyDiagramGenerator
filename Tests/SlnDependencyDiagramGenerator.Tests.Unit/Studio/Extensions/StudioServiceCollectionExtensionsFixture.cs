using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Extensions;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Studio.Extensions;

public class StudioServiceCollectionExtensionsFixture
{
    [Fact]
    public void Should_Not_Register_Marker_Interface_As_Service()
    {
        var services = new ServiceCollection();
        services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var marker = scope.ServiceProvider.GetService<IStudioScopedDependency>();

        marker.ShouldBeNull();
    }
}