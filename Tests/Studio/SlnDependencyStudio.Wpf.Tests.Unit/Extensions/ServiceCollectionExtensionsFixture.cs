using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.DependencyInjection;
using SlnDependencyStudio.Wpf.Extensions;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Extensions;

public class ServiceCollectionExtensionsFixture
{
    [Fact]
    public void Should_Register_IFileSystem()
    {
        var services = new ServiceCollection();

        services.AddWpfDependencies();

        var descriptor = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(IFileSystem));
        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(SystemFileSystem));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Should_Register_ScopedOperationFactory()
    {
        var services = new ServiceCollection();

        services.AddWpfDependencies();

        var descriptor = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(IScopedOperationFactory<>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
