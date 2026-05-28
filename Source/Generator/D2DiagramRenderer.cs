using SlnDependencyDiagramGenerator.Config;
using System.Text;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Renders a <see cref="DependencyGraphModel"/> as a D2 diagram file.</summary>
internal sealed class D2DiagramRenderer : DiagramRendererBase
{
    /// <inheritdoc />
    public override string FileExtension => "d2";

    /// <summary>Initializes a new D2 diagram renderer.</summary>
    /// <param name="options">The diagram options.</param>
    public D2DiagramRenderer(GeneratorDiagramOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override string Render(DependencyGraphModel model)
    {
        // Build once, render once: shared graph semantics come from the base class IR builder.
        var ir = BuildIntermediateRepresentation(model);
        var sb = new StringBuilder();

        sb.AppendLine($"direction: {D2Direction()}");
        sb.AppendLine();

        if (Options.Grouping.Enabled)
        {
            sb.AppendLine($"{Options.GroupNameAlias}: {Options.GroupName}");
        }

        foreach (var group in ir.Groups)
        {
            // D2 package-group container for multi-version package sets.
            sb.AppendLine($"{group.Alias}: \"\"");
        }

        foreach (var node in ir.Nodes)
        {
            var label = node.Version is null
                ? node.Label
                : $"{node.Label}\\nv{node.Version}";

            sb.AppendLine($"{node.Alias}: {label}");
        }

        foreach (var edge in ir.Edges)
        {
            sb.AppendLine($"{edge.ToAlias} <- {edge.FromAlias}");
        }

        foreach (var style in ir.Styles)
        {
            var (fill, opacity) = GetStyleValues(style.Role);

            sb.AppendLine($"{style.Alias}.style.fill: \"{fill}\"");
            sb.AppendLine($"{style.Alias}.style.opacity: {opacity}");
        }

        if (Options.Grouping.Enabled)
        {
            var (groupFill, groupOpacity) = GetGroupStyleValues();

            sb.AppendLine($"{Options.GroupNameAlias}.style.fill: \"{groupFill}\"");
            sb.AppendLine($"{Options.GroupNameAlias}.style.opacity: {groupOpacity}");

            foreach (var group in ir.Groups)
            {
                sb.AppendLine($"{group.Alias}.style.fill: \"{groupFill}\"");
                sb.AppendLine($"{group.Alias}.style.opacity: {groupOpacity}");
            }
        }

        sb.AppendLine();

        return sb.ToString();
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

    private (string Fill, double Opacity) GetGroupStyleValues()
    {
        return (Options.Grouping.BackgroundStyle.Fill, Options.Grouping.BackgroundStyle.Opacity);
    }
}
