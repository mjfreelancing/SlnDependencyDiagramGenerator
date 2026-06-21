using AllOverIt.Validation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.PreGeneration;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Shared.Tests.Unit.Extensions;

public class StudioServiceCollectionExtensionsFixture
{
    [Fact]
    public void Should_Not_Register_Marker_Interface_As_Service()
    {
        using var provider = CreateServiceProvider();

        using var scope = provider.CreateScope();
        var marker = scope.ServiceProvider.GetService<IStudioScopedDependency>();

        marker.ShouldBeNull();
    }

    [Fact]
    public void Should_Register_IValidationInvoker()
    {
        using var provider = CreateServiceProvider();

        var invoker = provider.GetService<IValidationInvoker>();

        invoker.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Register_IPreGenerationCommandRunner()
    {
        using var provider = CreateServiceProvider();

        using var scope = provider.CreateScope();
        var runner = scope.ServiceProvider.GetService<IPreGenerationCommandRunner>();

        runner.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Register_IDependencyProjectSerializer()
    {
        using var provider = CreateServiceProvider();

        using var scope = provider.CreateScope();
        var serializer = scope.ServiceProvider.GetService<IDependencyProjectSerializer>();

        serializer.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Register_DependencyGenerator()
    {
        using var provider = CreateServiceProvider();

        using var scope = provider.CreateScope();
        var generator = scope.ServiceProvider.GetService<IDependencyGenerator>();

        generator.ShouldNotBeNull();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        return services.BuildServiceProvider();
    }
}
