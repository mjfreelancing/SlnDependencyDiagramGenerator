using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;

namespace SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;

/// <inheritdoc cref="IPreGenerationCommandRunner"/>
internal sealed class PreGenerationCommandRunner : ProcessCommandRunnerBase<PreGenerationCommandResult>, IPreGenerationCommandRunner
{
    /// <inheritdoc />
    protected override PreGenerationCommandResult CreateResult(bool succeeded, int exitCode, string? errorMessage)
        => new()
        {
            Succeeded = succeeded,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };

    /// <inheritdoc />
    protected override string OperationName => "Pre-generation command";

    /// <inheritdoc />
    protected override int CancelledExitCode => StudioExitCode.PreGenerationCommandCancelled.Value;

    /// <inheritdoc />
    protected override int TimeoutExitCode => StudioExitCode.PreGenerationCommandTimeout.Value;

    /// <inheritdoc />
    protected override int UnexpectedErrorExitCode => StudioExitCode.PreGenerationUnexpectedError.Value;

    /// <summary>Initializes a new instance of <see cref="PreGenerationCommandRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PreGenerationCommandRunner(ILogger<PreGenerationCommandRunner> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public async Task<PreGenerationCommandResult> RunAsync(PreGenerationConfig config, CancellationToken cancellationToken)
    {
        if (!config.Enabled)
        {
            return new PreGenerationCommandResult
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
