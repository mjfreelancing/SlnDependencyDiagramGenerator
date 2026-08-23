namespace SlnDependencyStudio.Shared.Validators.Contexts;

/// <summary>Validation context supplying the directory used to resolve relative working-directory paths for pre-generation commands.</summary>
public sealed class PreGenerationConfigContext
{
    /// <summary>The directory used to resolve relative working-directory paths during validation.</summary>
    public string ProjectDirectory { get; init; } = string.Empty;
}
