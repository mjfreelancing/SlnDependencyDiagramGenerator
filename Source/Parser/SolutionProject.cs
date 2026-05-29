namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents a project discovered from the solution and its resolved dependencies.</summary>
internal sealed class SolutionProject
{
    /// <summary>The project name without file extension.</summary>
    public string Name { get; init; }

    /// <summary>The fully-qualified project path.</summary>
    public string Path { get; init; }

    /// <summary>The target frameworks discovered for the project.</summary>
    public string[] TargetFrameworks { get; init; }

    /// <summary>The project-to-project references resolved for this project.</summary>
    public ProjectReference[] ProjectReferences { get; init; }

    /// <summary>The framework references resolved for this project.</summary>
    public FrameworkReference[] FrameworkReferences { get; init; }

    /// <summary>The package references resolved for this project.</summary>
    public PackageReference[] PackageReferences { get; init; }
}