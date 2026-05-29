/*
 * Node: a diagram element such as a project, package, or framework reference.
 * Edge: a directed relationship from one node to another, such as a dependency or reference.
 */

using System;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;

/// <summary>
/// A renderer-agnostic intermediate representation for dependency diagrams.
/// </summary>
/// <remarks>
/// The goal of this intermediate representation is to separate graph semantics from renderer syntax.
/// D2, Mermaid, and future renderers can consume the same structure and only differ
/// in how they serialize nodes/edges/styles/groups to their specific text formats.
/// </remarks>
internal sealed class DiagramIntermediateRepresentation
{
    private readonly Dictionary<string, DiagramIrNode> _nodesByAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DiagramIrNode> _nodeOrder = [];

    private readonly HashSet<string> _edgeKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DiagramIrEdge> _edgeOrder = [];

    private readonly Dictionary<string, DiagramIrStyleRole> _stylesByAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _styleOrder = [];

    private readonly Dictionary<string, DiagramIrGroup> _groupsByAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DiagramIrGroup> _groupOrder = [];

    private readonly Dictionary<string, string> _nodeToGroup = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The de-duplicated nodes in insertion order.</summary>
    public IReadOnlyList<DiagramIrNode> Nodes => _nodeOrder;

    /// <summary>The de-duplicated edges in insertion order.</summary>
    public IReadOnlyList<DiagramIrEdge> Edges => _edgeOrder;

    /// <summary>The style entries in insertion order.</summary>
    public IReadOnlyList<DiagramIrStyle> Styles
    {
        get
        {
            var styles = new List<DiagramIrStyle>(_styleOrder.Count);

            foreach (var alias in _styleOrder)
            {
                styles.Add(new DiagramIrStyle(alias, _stylesByAlias[alias]));
            }

            return styles;
        }
    }

    /// <summary>The package grouping containers in insertion order.</summary>
    public IReadOnlyList<DiagramIrGroup> Groups => _groupOrder;

    /// <summary>Adds or updates a node by alias.</summary>
    /// <param name="alias">Renderer-neutral node alias.</param>
    /// <param name="label">Display label for the node.</param>
    /// <param name="version">Optional package version (used by package nodes).</param>
    public void AddNode(string alias, string label, string version = null)
    {
        if (_nodesByAlias.ContainsKey(alias))
        {
            return;
        }

        var node = new DiagramIrNode(alias, label, version);

        _nodesByAlias[alias] = node;
        _nodeOrder.Add(node);
    }

    /// <summary>Adds an edge if it has not already been registered.</summary>
    public void AddEdge(string fromAlias, string toAlias)
    {
        var edgeKey = $"{fromAlias}|{toAlias}";

        if (_edgeKeys.Add(edgeKey))
        {
            _edgeOrder.Add(new DiagramIrEdge(fromAlias, toAlias));
        }
    }

    /// <summary>
    /// Sets a style role for a node alias.
    /// </summary>
    /// <remarks>
    /// When <paramref name="allowOverride"/> is false, the existing style is preserved.
    /// This is used for transitive-package style writes where explicit style may arrive later.
    /// </remarks>
    public void SetStyleRole(string alias, DiagramIrStyleRole styleRole, bool allowOverride)
    {
        if (_stylesByAlias.ContainsKey(alias))
        {
            if (allowOverride)
            {
                _stylesByAlias[alias] = styleRole;
            }

            return;
        }

        _stylesByAlias[alias] = styleRole;
        _styleOrder.Add(alias);
    }

    /// <summary>Ensures a package group exists for multi-version package nodes.</summary>
    public void EnsureGroup(string groupAlias, string groupLabel)
    {
        if (_groupsByAlias.ContainsKey(groupAlias))
        {
            return;
        }

        var group = new DiagramIrGroup(groupAlias, groupLabel);

        _groupsByAlias[groupAlias] = group;
        _groupOrder.Add(group);
    }

    /// <summary>Adds a node alias to a group container.</summary>
    public void AddNodeToGroup(string groupAlias, string nodeAlias)
    {
        if (!_groupsByAlias.TryGetValue(groupAlias, out var group))
        {
            throw new InvalidOperationException($"Group '{groupAlias}' was not created before assigning node '{nodeAlias}'.");
        }

        if (_nodeToGroup.ContainsKey(nodeAlias))
        {
            return;
        }

        _nodeToGroup[nodeAlias] = groupAlias;
        group.NodeAliases.Add(nodeAlias);
    }

    /// <summary>Returns the group alias for a node alias, or <see langword="null"/> when ungrouped.</summary>
    public string GetNodeGroupAlias(string nodeAlias)
    {
        return _nodeToGroup.TryGetValue(nodeAlias, out var groupAlias)
            ? groupAlias
            : null;
    }
}
