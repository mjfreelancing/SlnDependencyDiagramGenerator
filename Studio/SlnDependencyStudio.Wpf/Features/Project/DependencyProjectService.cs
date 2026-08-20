using AllOverIt.Assertion;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>Default implementation of <see cref="IDependencyProjectService"/>.</summary>
internal sealed class DependencyProjectService : IDependencyProjectService
{
    private readonly IDependencyProjectSerializer _serializer;
    private readonly ILogger<DependencyProjectService> _logger;

    /// <summary>Initializes a new instance of <see cref="DependencyProjectService"/>.</summary>
    /// <param name="serializer">The serializer used to load and save dependency project documents.</param>
    /// <param name="logger">The logger instance.</param>
    public DependencyProjectService(IDependencyProjectSerializer serializer, ILogger<DependencyProjectService> logger)
    {
        _serializer = serializer;
        _logger = logger;
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
