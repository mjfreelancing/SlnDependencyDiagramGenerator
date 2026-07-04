using ReactiveUI.Builder;
using System;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

/// <summary>Initializes ReactiveUI for the test process.</summary>
public sealed class ReactiveUIInitializer : IDisposable
{
    private static bool Initialized;

    public ReactiveUIInitializer()
    {
        if (!Initialized)
        {
            RxAppBuilder
                .CreateReactiveUIBuilder()
                .WithCoreServices()
                .BuildApp();

            Initialized = true;
        }
    }

    public void Dispose()
    {
    }
}

[CollectionDefinition(nameof(ReactiveUIInitializer))]
public sealed class ReactiveUICollection : ICollectionFixture<ReactiveUIInitializer>
{
}
