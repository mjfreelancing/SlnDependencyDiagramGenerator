namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>
/// Logical style roles independent of renderer syntax.
/// </summary>
internal enum DiagramIrStyleRole
{
    /// <summary>Framework reference style.</summary>
    Framework,

    /// <summary>Explicit package reference style.</summary>
    PackageExplicit,

    /// <summary>Transitive package reference style.</summary>
    PackageTransitive
}
