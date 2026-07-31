namespace SlnDependencyStudio.Shared.Config;

/// <summary>Configuration for an optional pre-generation command that executes before diagram generation starts.</summary>
public sealed class PreGenerationConfig : ProcessCommandConfig
{
    /// <summary>When true, diagram generation proceeds even if the pre-generation command fails.</summary>
    public bool ContinueOnFailure { get; set; }
}
