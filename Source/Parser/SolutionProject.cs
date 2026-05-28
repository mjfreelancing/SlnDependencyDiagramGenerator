using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents a project discovered from the solution and its resolved dependencies.</summary>
internal sealed class SolutionProject
{
    /// <summary>The project name without file extension.</summary>
    public string Name { get; init; }

    /// <summary>The fully-qualified project path.</summary>
    public string Path { get; init; }

    /// <summary>The target frameworks discovered for the project.</summary>
    public IReadOnlyCollection<string> TargetFrameworks { get; init; }

    /// <summary>The dependency sets associated with this project.</summary>
    public IReadOnlyCollection<ConditionalReferences> Dependencies { get; init; }
}