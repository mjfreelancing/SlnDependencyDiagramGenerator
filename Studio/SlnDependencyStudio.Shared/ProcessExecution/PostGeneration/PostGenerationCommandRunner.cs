using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;

namespace SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;

/// <inheritdoc cref="IPostGenerationCommandRunner"/>
internal sealed class PostGenerationCommandRunner : ProcessCommandRunnerBase<PostGenerationCommandResult>, IPostGenerationCommandRunner
{
    /// <inheritdoc />
    protected override PostGenerationCommandResult CreateResult(bool succeeded, int exitCode, string? errorMessage)
        => new()
        {
            Succeeded = succeeded,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };

    /// <inheritdoc />
    protected override string OperationName => "Post-generation command";

    /// <inheritdoc />
    protected override int CancelledExitCode => StudioExitCode.PostGenerationCommandCancelled.Value;

    /// <inheritdoc />
    protected override int TimeoutExitCode => StudioExitCode.PostGenerationCommandTimeout.Value;

    /// <inheritdoc />
    protected override int UnexpectedErrorExitCode => StudioExitCode.PostGenerationCommandUnexpectedError.Value;

    /// <summary>Initializes a new instance of <see cref="PostGenerationCommandRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PostGenerationCommandRunner(ILogger<PostGenerationCommandRunner> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public async Task<PostGenerationCommandResult> RunAsync(PostGenerationConfig config, CancellationToken cancellationToken)
    {
        if (!config.Enabled)
        {
            return new PostGenerationCommandResult
            {
                Succeeded = true,
                ExitCode = 0
            };
        }

        // Split the arguments string so each token is passed as a separate argument to the process.
        // Without splitting, the entire string is treated as a single quoted argument
        // (e.g. "restore file.sln" will be seen by the process as a single unknown command name).
        var arguments = config.Arguments.IsNotNullOrEmpty()
            ? config.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

        return await RunAsync(config.Command, arguments, config.WorkingDirectory, cancellationToken).ConfigureAwait(false);
    }
}
