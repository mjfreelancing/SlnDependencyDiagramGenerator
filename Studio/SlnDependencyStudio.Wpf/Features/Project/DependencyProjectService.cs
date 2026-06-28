using AllOverIt.Assertion;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Wpf.Features.Project;

internal sealed class DependencyProjectService : IDependencyProjectService
{
    private readonly IDependencyProjectSerializer _serializer;

    public DependencyProjectService(IDependencyProjectSerializer serializer)
    {
        _serializer = serializer.WhenNotNull();
    }

    public async Task<DependencyProjectDocument> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        return await _serializer.DeserializeAsync(filePath, cancellationToken).ConfigureAwait(false);
    }
}
