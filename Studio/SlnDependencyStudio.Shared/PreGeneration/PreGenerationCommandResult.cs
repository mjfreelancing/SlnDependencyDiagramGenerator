namespace SlnDependencyStudio.Shared.PreGeneration;

/// <summary>Describes the outcome of a pre-generation command execution attempt.</summary>
public sealed class PreGenerationCommandResult
{
    /// <summary>Whether the command completed successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>The process exit code. Zero indicates success; non-zero indicates failure.</summary>
    public int ExitCode { get; init; }

    /// <summary>An error message when the command failed or was cancelled, otherwise <see langword="null"/>.</summary>
    public string? ErrorMessage { get; init; }
}
