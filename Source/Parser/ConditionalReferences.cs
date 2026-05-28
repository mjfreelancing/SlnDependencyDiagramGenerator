using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents dependency references associated with a project for a specific condition.</summary>
internal sealed class ConditionalReferences
{
    /// <summary>The evaluated condition for this dependency set, or an empty string when unconditional.</summary>
    public string Condition { get; init; }

    /// <summary>The project-to-project references declared for this dependency set.</summary>
    public IReadOnlyCollection<ProjectReference> ProjectReferences { get; init; }

    /// <summary>The framework references declared for this dependency set.</summary>
    public IReadOnlyCollection<FrameworkReference> FrameworkReferences { get; init; }

    /// <summary>The resolved package references declared for this dependency set.</summary>
    public IReadOnlyCollection<PackageReference> PackageReferences { get; init; }
}