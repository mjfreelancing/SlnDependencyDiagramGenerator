using Microsoft.Extensions.Logging;

namespace SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;

/// <inheritdoc cref="IRestoreSolutionRunner"/>
internal sealed class RestoreSolutionRunner : ProcessCommandRunnerBase<RestoreSolutionResult>, IRestoreSolutionRunner
{
    /// <inheritdoc />
    protected override RestoreSolutionResult CreateResult(CommandErrorCode errorCode, int? exitCode, string? errorMessage)
        => new()
        {
            ErrorCode = errorCode,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };

    /// <inheritdoc />
    protected override string OperationName => "Solution restore";

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
