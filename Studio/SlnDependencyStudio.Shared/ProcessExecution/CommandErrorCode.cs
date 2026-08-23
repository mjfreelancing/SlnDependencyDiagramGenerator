namespace SlnDependencyStudio.Shared.ProcessExecution;

/// <summary>
/// Classifies the outcome of a pipeline command execution. Kept separate from the process
/// <see cref="ProcessCommandResult.ExitCode"/> so an internal classification can never be
/// confused with an exit code returned by the process itself.
/// </summary>
public enum CommandErrorCode
{
    /// <summary>The command completed successfully.</summary>
    None = 0,

    /// <summary>The command was cancelled before or during execution.</summary>
    Cancelled,

    /// <summary>An unexpected error occurred (for example, the process could not be started).</summary>
    UnexpectedError,

    /// <summary>The process ran to completion and exited with a non-zero exit code.</summary>
    ProcessExitedWithFailure
}
