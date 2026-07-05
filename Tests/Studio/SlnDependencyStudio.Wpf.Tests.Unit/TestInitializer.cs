using ReactiveUI;
using ReactiveUI.Builder;
using System;
using System.Reactive.Concurrency;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

/// <summary>Initializes ReactiveUI for the test process.</summary>
public sealed class ReactiveUIInitializer : IDisposable
{
    private static bool Initialized;

    public ReactiveUIInitializer()
    {
        if (!Initialized)
        {
            // Use Immediate schedulers so ReactiveCommand observables emit
            // synchronously, allowing xUnit to capture exceptions on the same call stack.
            RxAppBuilder
                .CreateReactiveUIBuilder()
                .WithMainThreadScheduler(ImmediateScheduler.Instance)
                .WithTaskPoolScheduler(ImmediateScheduler.Instance)
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
