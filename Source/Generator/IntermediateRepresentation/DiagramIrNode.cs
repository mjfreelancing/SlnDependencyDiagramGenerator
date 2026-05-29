namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a single graph node.</summary>
internal sealed record DiagramIrNode(string Alias, string Label, string Version);
