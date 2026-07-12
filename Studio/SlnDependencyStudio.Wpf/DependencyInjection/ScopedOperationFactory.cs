using Microsoft.Extensions.DependencyInjection;

namespace SlnDependencyStudio.Wpf.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IScopedOperationFactory{TService}"/>.
/// Wraps <see cref="IServiceScopeFactory"/> to manage scope lifetime.
/// </summary>
internal sealed class ScopedOperationFactory<TService> : IScopedOperationFactory<TService>
    where TService : notnull
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Initializes a new instance of <see cref="ScopedOperationFactory{TService}"/>.</summary>
    public ScopedOperationFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(Func<TService, TResult> operation)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();

        return operation.Invoke(service);
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();

        return await operation.Invoke(service, cancellationToken).ConfigureAwait(false);
    }
}
