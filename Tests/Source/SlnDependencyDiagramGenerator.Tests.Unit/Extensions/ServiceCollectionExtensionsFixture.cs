using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using System.Linq;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Extensions;

public class ServiceCollectionExtensionsFixture
{
    [Fact]
    public void Should_Register_SolutionProjectResolvers()
    {
        var provider = CreateServiceProvider();

        var resolvers = provider.GetServices<ISolutionProjectResolver>().ToArray();
        resolvers.Length.ShouldBe(2);
        resolvers.ShouldContain(resolver => resolver is SlnSolutionProjectResolver);
        resolvers.ShouldContain(resolver => resolver is SlnxSolutionProjectResolver);
    }

    [Fact]
    public void Should_Register_SolutionParser_As_Scoped()
    {
        var provider = CreateServiceProvider();

        var instance = provider.GetService<ISolutionParser>();
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<SolutionParser>();
    }

    [Fact]
    public void Should_Register_ProjectDiscoveryService_As_Scoped()
    {
        var provider = CreateServiceProvider();

        var instance = provider.GetService<IProjectDiscoveryService>();
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ProjectDiscoveryService>();
    }

    [Fact]
    public void Should_Register_ToolDetectionService_As_Singleton()
    {
        var services = new ServiceCollection();
        services.AddSlnDependencyDiagramGenerator();

        var descriptor = services.Single(service => service.ServiceType == typeof(IToolDetectionService));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(ToolDetectionService));
    }

    [Fact]
    public void Should_Register_ToolPathOverridesProvider_As_Singleton()
    {
        var provider = CreateServiceProvider();

        var instance1 = provider.GetRequiredService<ToolPathOverridesProvider>();
        var instance2 = provider.GetRequiredService<ToolPathOverridesProvider>();

        instance1.ShouldNotBeNull();
        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void Should_Register_ToolPathResolver_As_Singleton()
    {
        var services = new ServiceCollection();
        services.AddSlnDependencyDiagramGenerator();

        var descriptor = services.Single(service => service.ServiceType == typeof(IToolPathResolver));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(ToolPathResolver));
    }

    [Fact]
    public void Should_Register_DependencyGenerator_As_Scoped()
    {
        var provider = CreateServiceProvider();

        var instance = provider.GetService<IDependencyGenerator>();
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<DependencyGenerator>();
    }

    [Fact]
    public void Should_Register_ProjectAssetReader_As_Scoped()
    {
        var provider = CreateServiceProvider();

        var instance = provider.GetService<IProjectAssetReader>();
        instance.ShouldNotBeNull();
        instance.ShouldBeOfType<ProjectAssetReader>();
    }

    [Fact]
    public void Should_Return_Registration_With_Services_And_ValidationRegistry()
    {
        var services = new ServiceCollection();

        var registration = services.AddSlnDependencyDiagramGenerator();

        registration.Services.ShouldBeSameAs(services);
        registration.ValidationRegistry.ShouldNotBeNull();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSlnDependencyDiagramGenerator();

        return services.BuildServiceProvider();
    }
}
