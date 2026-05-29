using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>Represents a logical grouping container (used for multi-version package sets).</summary>
internal sealed class DiagramIrGroup
{
    /// <summary>Initializes a new group container.</summary>
    public DiagramIrGroup(string alias, string label)
    {
        Alias = alias;
        Label = label;
    }

    /// <summary>The group alias.</summary>
    public string Alias { get; }

    /// <summary>The group display label.</summary>
    public string Label { get; }

    /// <summary>The node aliases that belong to this group.</summary>
    public List<string> NodeAliases { get; } = [];
}
