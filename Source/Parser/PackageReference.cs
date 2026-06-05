namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents an explicit or transitive package dependency.</summary>
public sealed class PackageReference
{
    /// <summary>Indicates whether the package is a transitive dependency.</summary>
    public bool IsTransitive { get; }

    /// <summary>The dependency depth (0 for explicit packages, 1 or more for transitive packages).</summary>
    public int Depth { get; }

    /// <summary>The package identifier.</summary>
    public string Name { get; init; }

    /// <summary>The resolved package version.</summary>
    public string Version { get; init; }

    /// <summary>
    /// The version range requested by the parent dependency edge.
    /// </summary>
    /// <remarks>
    /// This is <see langword="null"/> for explicit package references.
    /// </remarks>
    public string RequestedVersionRange { get; init; }

    /// <summary>
    /// Indicates whether the parent edge requested a specific version that differs from the resolved version.
    /// </summary>
    public bool RequestedDifferentVersion { get; init; }

    /// <summary>The transitive package dependencies.</summary>
    public PackageReference[] TransitiveReferences { get; init; }

    /// <summary>Initializes a new explicit package reference.</summary>
    public PackageReference()
        : this(false, 0)
    {
    }

    /// <summary>Initializes a package reference with the specified transitive state and depth.</summary>
    /// <param name="isTransitive">Indicates whether the package is transitive.</param>
    /// <param name="depth">The dependency depth.</param>
    public PackageReference(bool isTransitive, int depth)
    {
        IsTransitive = isTransitive;
        Depth = depth;
    }
}