namespace SlnDependencyStudio.Shared.Services;

/// <summary>Result of a generation run via <see cref="IStudioGenerationService"/>.</summary>
public sealed class GenerationResult
{
    /// <summary>Indicates whether diagram generation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Indicates whether the pre-generation command was configured and executed.</summary>
    public bool PreGenerationRan { get; init; }

    /// <summary>Indicates whether the pre-generation command succeeded (always <see langword="true"/> when not run).</summary>
    public bool PreGenerationSucceeded { get; init; } = true;

    /// <summary>An error message when <see cref="Success"/> is <see langword="false"/>.</summary>
    public string? ErrorMessage { get; init; }
}