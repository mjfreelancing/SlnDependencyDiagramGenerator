namespace SlnDependencyStudio.Shared.Validators.Contexts;

/// <summary>Validation context supplying the directory used to resolve relative working-directory paths for post-generation commands.</summary>
public sealed class PostGenerationConfigContext
{
    /// <summary>The directory used to resolve relative working-directory paths during validation.</summary>
    public string ProjectDirectory { get; init; } = string.Empty;
}
