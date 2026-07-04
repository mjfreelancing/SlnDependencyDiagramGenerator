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

    /// <inheritdoc />
    public DependencyProjectDocument CreateFromDefaults()
    {
        return new DependencyProjectDocument();
    }

    /// <inheritdoc />
    public Task<DependencyProjectDocument> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        return _serializer.DeserializeAsync(filePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync(DependencyProjectDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        return _serializer.SerializeAsync(document, filePath, cancellationToken);
    }
}
