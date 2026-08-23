namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a directional graph edge.</summary>
/// <param name="FromAlias">The alias of the source node.</param>
/// <param name="ToAlias">The alias of the target node.</param>
internal sealed record DiagramIrEdge(string FromAlias, string ToAlias);
