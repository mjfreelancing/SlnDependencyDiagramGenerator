namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a style role attached to a node alias.</summary>
/// <param name="Alias">The node alias the style applies to.</param>
/// <param name="Role">The logical style role.</param>
internal sealed record DiagramIrStyle(string Alias, DiagramIrStyleRole Role);
