using AllOverIt.Assertion;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Utils;

namespace SlnDependencyStudio.Cli.Handlers;

internal abstract class CommandLineHandlerBase
{
    private readonly IDependencyProjectSerializer _serializer;
    private readonly ILogger _logger;

    public CommandLineHandlerBase(IDependencyProjectSerializer serializer, ILogger logger)
    {
        _serializer = serializer.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    public abstract Task HandleAsync(string configFilename, CancellationToken cancellationToken);

    protected async Task<DependencyProjectDocument> LoadDependencyProjectDocumentAsync(string configFilename, CancellationToken cancellationToken)
    {
        var document = await _serializer.DeserializeAsync(configFilename, cancellationToken);

        // Convert all relative paths to absolute paths to avoid downstream handling.
        var configDirectory = GetConfigDirectory(configFilename);
        document.DiagramGenerator.Projects.SolutionPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Projects.SolutionPath, configDirectory);
        document.DiagramGenerator.Export.RootPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Export.RootPath, configDirectory);

        return document;
    }

    protected void WriteValidationErrors(ValidationException exception)
    {
        _logger.LogError("Configuration validation failed:");

        foreach (var error in exception.Errors)
        {
            _logger.LogError("  - {ErrorMessage}", error.ErrorMessage);
        }
    }

    protected static string GetConfigDirectory(string configFilePath)
    {
        return Path.GetDirectoryName(Path.GetFullPath(configFilePath))
            ?? throw new DirectoryNotFoundException($"Cannot determine directory from path: {configFilePath}");
    }
}
