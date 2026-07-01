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

    public Task<DependencyProjectDocument> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        return _serializer.DeserializeAsync(filePath, cancellationToken);
    }

    public Task SaveAsync(DependencyProjectDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        return _serializer.SerializeAsync(document, filePath, cancellationToken);
    }
}
