namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a single graph node.</summary>
/// <param name="Alias">The renderer-neutral node alias.</param>
/// <param name="Label">The display label for the node.</param>
/// <param name="Version">An optional package version (used by package nodes).</param>
internal sealed record DiagramIrNode(string Alias, string Label, string? Version);
