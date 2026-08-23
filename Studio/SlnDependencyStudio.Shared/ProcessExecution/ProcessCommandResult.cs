namespace SlnDependencyStudio.Shared.ProcessExecution;

/// <summary>
/// Base class describing the outcome of a pipeline command execution attempt.
/// The process <see cref="ExitCode"/> is kept distinct from the internal <see cref="ErrorCode"/> so that
/// a process returning any value (including one that happens to match an internal code) can never be
/// confused with a command that was cancelled or failed unexpectedly.
/// </summary>
public abstract class ProcessCommandResult
{
    /// <summary>Whether the command completed successfully.</summary>
    public bool Succeeded => ErrorCode == CommandErrorCode.None;

    /// <summary>The failure classification. <see cref="CommandErrorCode.None"/> indicates success.</summary>
    public CommandErrorCode ErrorCode { get; init; } = CommandErrorCode.None;

    /// <summary>The exit code returned by the process, or <see langword="null"/> when the process did not run to completion.</summary>
    public int? ExitCode { get; init; }

    /// <summary>An error message when the command failed or was cancelled, otherwise <see langword="null"/>.</summary>
    public string? ErrorMessage { get; init; }
}
