using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Enumerations;

namespace SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;

/// <inheritdoc cref="IRestoreSolutionRunner"/>
internal sealed class RestoreSolutionRunner : ProcessCommandRunnerBase<RestoreSolutionResult>, IRestoreSolutionRunner
{
    /// <inheritdoc />
    protected override RestoreSolutionResult CreateResult(bool succeeded, int exitCode, string? errorMessage)
        => new()
        {
            Succeeded = succeeded,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };

    /// <inheritdoc />
    protected override string OperationName => "Solution restore";

    /// <inheritdoc />
    protected override int CancelledExitCode => StudioExitCode.PreGenerationCommandCancelled.Value;

    /// <inheritdoc />
    protected override int TimeoutExitCode => StudioExitCode.PreGenerationCommandTimeout.Value;

    /// <inheritdoc />
    protected override int UnexpectedErrorExitCode => StudioExitCode.DotNetRestoreFailed.Value;

    /// <summary>Initializes a new instance of <see cref="RestoreSolutionRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public RestoreSolutionRunner(ILogger<RestoreSolutionRunner> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public async Task<RestoreSolutionResult> RunAsync(string solutionPath, CancellationToken cancellationToken)
    {
        return await RunAsync("dotnet", ["restore", solutionPath], null, cancellationToken).ConfigureAwait(false);
    }
}
