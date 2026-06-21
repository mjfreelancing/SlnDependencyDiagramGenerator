namespace SlnDependencyStudio.Shared.Config;

/// <summary>User-facing metadata stored in a dependency project document.</summary>
public sealed class DependencyProjectMetadata
{
    /// <summary>The display name of the dependency project.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>An optional description of the project's purpose or scope.</summary>
    public string Description { get; set; } = string.Empty;
}