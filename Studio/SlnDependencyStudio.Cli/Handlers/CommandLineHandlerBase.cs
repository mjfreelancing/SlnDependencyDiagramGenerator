using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Utils;

namespace SlnDependencyStudio.Cli.Handlers;

/// <summary>Base class for command-line handlers that load and validate dependency project documents.</summary>
internal abstract class CommandLineHandlerBase
{
    private readonly IDependencyProjectSerializer _serializer;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineHandlerBase"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineHandlerBase(IDependencyProjectSerializer serializer, ILogger logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    /// <summary>Handles the command with the given configuration file and cancellation token.</summary>
    /// <param name="configFilename">The path to the configuration file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the process exit code: 0 on success, or a
    /// <see cref="Enumerations.StudioCliExitCode"/> value on failure.</returns>
    public abstract Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken);

    /// <summary>Loads and resolves a dependency project document from a configuration file.</summary>
    /// <param name="configFilename">The path to the configuration file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized document with all relative paths resolved to absolute paths.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory of the config file cannot be determined.</exception>
    protected async Task<DependencyProjectDocument> LoadDependencyProjectDocumentAsync(string configFilename, CancellationToken cancellationToken)
    {
        var document = await _serializer.DeserializeAsync(configFilename, cancellationToken);

        // Convert all relative paths to absolute paths to avoid downstream handling.
        var configDirectory = GetConfigDirectory(configFilename);

        document.PreGeneration.WorkingDirectory = PathUtils.ResolveAsAbsolutePath(document.PreGeneration.WorkingDirectory, configDirectory);
        document.PostGeneration.WorkingDirectory = PathUtils.ResolveAsAbsolutePath(document.PostGeneration.WorkingDirectory, configDirectory);

        document.DiagramGenerator.Solution.SolutionPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Solution.SolutionPath, configDirectory);
        document.DiagramGenerator.Export.RootPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Export.RootPath, configDirectory);

        return document;
    }

    /// <summary>Logs validation errors to the logger.</summary>
    /// <param name="exception">The validation exception containing the errors.</param>
    protected void WriteValidationErrors(ValidationException exception)
    {
        _logger.LogError("Configuration validation failed:");

        foreach (var error in exception.Errors)
        {
            _logger.LogError("  - {ErrorMessage}", error.ErrorMessage);
        }
    }

    /// <summary>Gets the directory portion of a configuration file path.</summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    /// <returns>The fully-qualified directory path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory cannot be determined from the path.</exception>
    protected static string GetConfigDirectory(string configFilePath)
    {
        return Path.GetDirectoryName(Path.GetFullPath(configFilePath))
            ?? throw new DirectoryNotFoundException($"Cannot determine directory from path: {configFilePath}");
    }
}
