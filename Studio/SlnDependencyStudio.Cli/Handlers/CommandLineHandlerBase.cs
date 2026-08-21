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

    // Deliberately non-generic ILogger: this base class is abstract and has no type of its own, so derived
    // handlers pass their concrete ILogger<THandler> (e.g. ILogger<CommandLineRunHandler>) - the log
    // category then reflects the concrete handler rather than this base class.
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineHandlerBase"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineHandlerBase(IDependencyProjectSerializer serializer, ILogger logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    /// <summary>Handles the command with the given project file and cancellation token.</summary>
    /// <param name="projectFilename">The path to the project file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the process exit code: 0 on success, or a
    /// <see cref="Enumerations.StudioCliExitCode"/> value on failure.</returns>
    public abstract Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken);

    /// <summary>Loads and resolves a dependency project document from a project file.</summary>
    /// <param name="projectFilename">The path to the project file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized document with all relative paths resolved to absolute paths.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory of the project file cannot be determined.</exception>
    protected async Task<DependencyProjectDocument> LoadDependencyProjectDocumentAsync(string projectFilename, CancellationToken cancellationToken)
    {
        var document = await _serializer.DeserializeAsync(projectFilename, cancellationToken);

        // Convert all relative paths to absolute paths to avoid downstream handling.
        var projectDirectory = GetProjectDirectory(projectFilename);

        document.PreGeneration.WorkingDirectory = PathUtils.ResolveAsAbsolutePath(document.PreGeneration.WorkingDirectory, projectDirectory);
        document.PostGeneration.WorkingDirectory = PathUtils.ResolveAsAbsolutePath(document.PostGeneration.WorkingDirectory, projectDirectory);

        document.DiagramGenerator.Solution.SolutionPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Solution.SolutionPath, projectDirectory);
        document.DiagramGenerator.Export.RootPath = PathUtils.ResolveAsAbsolutePath(document.DiagramGenerator.Export.RootPath, projectDirectory);

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

    /// <summary>Gets the directory portion of a project file path.</summary>
    /// <param name="projectFilePath">The path to the project file.</param>
    /// <returns>The fully-qualified directory path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory cannot be determined from the path.</exception>
    protected static string GetProjectDirectory(string projectFilePath)
    {
        return Path.GetDirectoryName(Path.GetFullPath(projectFilePath))
            ?? throw new DirectoryNotFoundException($"Cannot determine directory from path: {projectFilePath}");
    }
}
