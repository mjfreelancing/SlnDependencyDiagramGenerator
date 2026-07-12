namespace SlnDependencyStudio.Wpf.DependencyInjection;

/// <summary>
/// Type-safe scoped operation factory. Resolves <typeparamref name="TService"/> within
/// a new DI scope for the duration of the operation, then disposes the scope.
/// Keeps dependency visibility explicit while encapsulating scope lifetime management.
/// </summary>
/// <typeparam name="TService">The scoped service type to resolve.</typeparam>
public interface IScopedOperationFactory<TService> where TService : notnull
{
    /// <summary>
    /// Creates a scope, resolves <typeparamref name="TService"/>, invokes
    /// <paramref name="operation"/>, and disposes the scope.
    /// </summary>
    TResult Execute<TResult>(Func<TService, TResult> operation);

    /// <summary>
    /// Creates a scope, resolves <typeparamref name="TService"/>, invokes
    /// <paramref name="operation"/>, and disposes the scope.
    /// </summary>
    Task ExecuteAsync(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a scope, resolves <typeparamref name="TService"/>, invokes
    /// <paramref name="operation"/>, and disposes the scope.
    /// </summary>
    Task<TResult> ExecuteAsync<TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
}
