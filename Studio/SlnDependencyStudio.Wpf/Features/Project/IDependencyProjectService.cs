using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>Service for managing the lifecycle of a dependency project document.</summary>
public interface IDependencyProjectService : IStudioScopedDependency
{
    /// <summary>Opens and deserializes a dependency project from the specified file path.</summary>
    /// <param name="filePath">The path to the <c>.sds</c> file.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>The deserialized <see cref="DependencyProjectDocument"/>.</returns>
    Task<DependencyProjectDocument> OpenAsync(string filePath, CancellationToken cancellationToken = default);
}
