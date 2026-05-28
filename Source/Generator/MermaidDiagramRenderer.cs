using SlnDependencyDiagramGenerator.Config;
using System;
using System.Text;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Renders a <see cref="DependencyGraphModel"/> as a Mermaid flowchart (.mmd) file.</summary>
internal sealed class MermaidDiagramRenderer : DiagramRendererBase
{
    /// <inheritdoc />
    public override string FileExtension => "mmd";

    /// <summary>Initializes a new Mermaid diagram renderer.</summary>
    /// <param name="options">The diagram options.</param>
    public MermaidDiagramRenderer(GeneratorDiagramOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override string Render(DependencyGraphModel model)
    {
        // Build once from the shared traversal so Mermaid and D2 stay semantically aligned.
        var ir = BuildIntermediateRepresentation(model);
        var sb = new StringBuilder();
        var groupingEnabled = Options.Grouping.Enabled;
        var projectGroupAlias = MermaidSafeAlias(Options.GroupNameAlias);

        sb.AppendLine($"flowchart {MermaidDirection()}");

        if (groupingEnabled)
        {
            sb.AppendLine($"  subgraph {projectGroupAlias}[\"{Options.GroupName}\"]");
            sb.AppendLine($"    direction {MermaidDirection()}");

            foreach (var node in ir.Nodes)
            {
                if (!IsProjectNode(node.Alias))
                {
                    continue;
                }

                var projectAlias = MermaidSafeAlias(node.Alias);
                sb.AppendLine($"    {projectAlias}[\"{node.Label}\"]");
            }

            sb.AppendLine("  end");
        }

        foreach (var node in ir.Nodes)
        {
            var nodeGroupAlias = ir.GetNodeGroupAlias(node.Alias);
            var shouldRenderNode = nodeGroupAlias is null && (!groupingEnabled || !IsProjectNode(node.Alias));

            if (shouldRenderNode)
            {
                var nodeAlias = MermaidSafeAlias(node.Alias);
                var nodeLabel = GetMermaidLabel(node);
                sb.AppendLine($"  {nodeAlias}[\"{nodeLabel}\"]");
            }
        }

        foreach (var group in ir.Groups)
        {
            var safeGroupAlias = MermaidSafeAlias(group.Alias);
            sb.AppendLine($"  subgraph {safeGroupAlias}[\"{group.Label}\"]");

            foreach (var groupNodeAlias in group.NodeAliases)
            {
                var groupNode = FindNode(ir, groupNodeAlias);
                var safeNodeAlias = MermaidSafeAlias(groupNode.Alias);
                var groupNodeLabel = GetMermaidLabel(groupNode);

                sb.AppendLine($"      {safeNodeAlias}[\"{groupNodeLabel}\"]");
            }

            sb.AppendLine("    end");
        }

        foreach (var edge in ir.Edges)
        {
            var fromAlias = MermaidSafeAlias(edge.FromAlias);
            var toAlias = MermaidSafeAlias(edge.ToAlias);

            sb.AppendLine($"  {fromAlias} --> {toAlias}");
        }

        if (groupingEnabled)
        {
            var (groupFill, groupStroke, groupOpacity) = GetGroupStyleValues();

            sb.AppendLine($"  style {projectGroupAlias} fill:{groupFill},stroke:{groupStroke},stroke-width:1px,opacity:{groupOpacity}");

            foreach (var group in ir.Groups)
            {
                var groupAlias = MermaidSafeAlias(group.Alias);

                sb.AppendLine($"  style {groupAlias} fill:{groupFill},stroke:{groupStroke},stroke-width:1px,opacity:{groupOpacity}");
            }
        }

        foreach (var style in ir.Styles)
        {
            var (fill, opacity) = GetStyleValues(style.Role);
            var styleAlias = MermaidSafeAlias(style.Alias);

            sb.AppendLine($"  style {styleAlias} fill:{fill},opacity:{opacity}");
        }

        return sb.ToString();
    }

    private static DiagramIrNode FindNode(DiagramIntermediateRepresentation ir, string alias)
    {
        foreach (var node in ir.Nodes)
        {
            if (node.Alias == alias)
            {
                return node;
            }
        }

        return null;
    }

    private static string GetMermaidLabel(DiagramIrNode node)
    {
        return node.Version is null
            ? node.Label
            : $"{node.Label}<br>v{node.Version}";
    }

    private bool IsProjectNode(string alias)
    {
        return alias.StartsWith($"{Options.GroupNameAlias}.", StringComparison.Ordinal);
    }

    private (string Fill, double Opacity) GetStyleValues(DiagramIrStyleRole styleRole)
    {
        return styleRole switch
        {
            DiagramIrStyleRole.Framework => (Options.FrameworkStyle.Fill, Options.FrameworkStyle.Opacity),
            DiagramIrStyleRole.PackageExplicit => (Options.PackageStyle.Fill, Options.PackageStyle.Opacity),
            _ => (Options.TransitiveStyle.Fill, Options.TransitiveStyle.Opacity)
        };
    }

    private (string Fill, string Stroke, double Opacity) GetGroupStyleValues()
    {
        var groupFill = Options.Grouping.BackgroundStyle.Fill;
        var groupOpacity = Options.Grouping.BackgroundStyle.Opacity;

        return (groupFill, groupFill, groupOpacity);
    }

    /// <summary>Mermaid node IDs may not contain dots — replace with underscores.</summary>
    private static string MermaidSafeAlias(string alias) => alias.Replace(".", "_");
}
