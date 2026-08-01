using AllOverIt.Assertion;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Wpf.Features.Project;

internal sealed class DependencyProjectService : IDependencyProjectService
{
    private readonly IDependencyProjectSerializer _serializer;
    private readonly ILogger<DependencyProjectService> _logger;

    public DependencyProjectService(IDependencyProjectSerializer serializer, ILogger<DependencyProjectService> logger)
    {
        _serializer = serializer.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public DependencyProjectDocument CreateFromDefaults()
    {
        _logger.LogDebug("Creating a new project from defaults");

        return new DependencyProjectDocument();
    }

    /// <inheritdoc />
    public Task<DependencyProjectDocument> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        _logger.LogDebug("Opening project from {FilePath}", filePath);

        return _serializer.DeserializeAsync(filePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync(DependencyProjectDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        _logger.LogDebug("Saving project to {FilePath}", filePath);

        return _serializer.SerializeAsync(document, filePath, cancellationToken);
    }
}
