namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a directional graph edge.</summary>
internal sealed record DiagramIrEdge(string FromAlias, string ToAlias);
