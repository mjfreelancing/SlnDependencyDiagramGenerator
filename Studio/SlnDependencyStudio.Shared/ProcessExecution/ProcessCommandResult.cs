namespace SlnDependencyStudio.Shared.ProcessExecution;

/// <summary>Base class describing the outcome of a pipeline command execution attempt.</summary>
public abstract class ProcessCommandResult
{
    /// <summary>Whether the command completed successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>The process exit code. Zero indicates success; non-zero indicates failure.</summary>
    public int ExitCode { get; init; }

    /// <summary>An error message when the command failed or was cancelled, otherwise <see langword="null"/>.</summary>
    public string? ErrorMessage { get; init; }
}
