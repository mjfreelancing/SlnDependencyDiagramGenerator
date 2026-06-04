using AllOverIt.Logging;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Renderers.Mermaid;

/// <summary>Renders a <see cref="DependencyGraphModel"/> as a Mermaid flowchart (.mmd) file.</summary>
internal sealed class MermaidDiagramRenderer : DiagramRendererBase
{
    private const string MermaidCliToolName = "mmdc";
    private const string ToolNotFoundMessage = "'mmdc' was not found on PATH. See: https://github.com/mermaid-js/mermaid-cli#installation";

    /// <inheritdoc />
    public override string FileExtension => "mmd";

    /// <summary>Initializes a new Mermaid diagram renderer.</summary>
    /// <param name="options">The diagram options.</param>
    /// <param name="logger">A logger for progress and diagnostics.</param>
    public MermaidDiagramRenderer(GeneratorDiagramOptions options, IColorConsoleLogger logger)
        : base(options, logger)
    {
    }

    /// <inheritdoc />
    public override async Task ValidateRequiredToolsAsync(bool imageExportEnabled, CancellationToken cancellationToken)
    {
        if (!imageExportEnabled)
        {
            return;
        }

        await EnsureToolAvailableAsync(MermaidCliToolName, ToolNotFoundMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string Render(DependencyGraphModel model)
    {
        // Build once from the shared traversal so Mermaid and D2 stay semantically aligned.
        var diagramRepresentation = BuildIntermediateRepresentation(model);
        var sb = new StringBuilder();
        var groupingEnabled = Options.Grouping.Enabled;
        var projectGroupAlias = MermaidSafeAlias(Options.GroupNameAlias);

        sb.AppendLine($"flowchart {GetDirection()}");

        if (groupingEnabled)
        {
            sb.AppendLine($"  subgraph {projectGroupAlias}[\"{Options.GroupName}\"]");
            sb.AppendLine($"    direction {GetDirection()}");

            foreach (var node in diagramRepresentation.Nodes)
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

        foreach (var node in diagramRepresentation.Nodes)
        {
            var nodeGroupAlias = diagramRepresentation.GetNodeGroupAlias(node.Alias);
            var shouldRenderNode = nodeGroupAlias is null && (!groupingEnabled || !IsProjectNode(node.Alias));

            if (shouldRenderNode)
            {
                var nodeAlias = MermaidSafeAlias(node.Alias);
                var nodeLabel = GetMermaidLabel(node);
                sb.AppendLine($"  {nodeAlias}[\"{nodeLabel}\"]");
            }
        }

        foreach (var group in diagramRepresentation.Groups)
        {
            var safeGroupAlias = MermaidSafeAlias(group.Alias);
            sb.AppendLine($"  subgraph {safeGroupAlias}[\"{group.Label}\"]");

            foreach (var groupNodeAlias in group.NodeAliases)
            {
                var groupNode = FindNode(diagramRepresentation, groupNodeAlias);
                var safeNodeAlias = MermaidSafeAlias(groupNode.Alias);
                var groupNodeLabel = GetMermaidLabel(groupNode);

                sb.AppendLine($"      {safeNodeAlias}[\"{groupNodeLabel}\"]");
            }

            sb.AppendLine("    end");
        }

        foreach (var edge in diagramRepresentation.Edges)
        {
            var fromAlias = MermaidSafeAlias(edge.FromAlias);
            var toAlias = MermaidSafeAlias(edge.ToAlias);

            sb.AppendLine($"  {fromAlias} --> {toAlias}");
        }

        if (groupingEnabled)
        {
            var (groupFill, groupStroke, groupOpacity) = GetGroupStyleValues();

            sb.AppendLine($"  style {projectGroupAlias} fill:{groupFill},stroke:{groupStroke},stroke-width:1px,opacity:{groupOpacity}");

            foreach (var group in diagramRepresentation.Groups)
            {
                var groupAlias = MermaidSafeAlias(group.Alias);

                sb.AppendLine($"  style {groupAlias} fill:{groupFill},stroke:{groupStroke},stroke-width:1px,opacity:{groupOpacity}");
            }
        }

        foreach (var style in diagramRepresentation.Styles)
        {
            var (fill, opacity) = GetStyleValues(style.Role);
            var styleAlias = MermaidSafeAlias(style.Alias);

            sb.AppendLine($"  style {styleAlias} fill:{fill},opacity:{opacity}");
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override string GetDirection()
    {
        return Options.Direction.ToString();
    }

    /// <inheritdoc />
    protected override async Task ExportImageFileAsync(string diagramFileName, DiagramImageFormat format, CancellationToken cancellationToken)
    {
        var imageFileName = Path.ChangeExtension(diagramFileName, format.ToString().ToLowerInvariant());

        Logger
            .Write(ConsoleColor.White, "Creating image: ")
            .Write(ConsoleColor.Yellow, Path.GetFileName(imageFileName))
            .Write(ConsoleColor.White, "...");

        var stopwatch = Stopwatch.StartNew();

        // On Windows, npm installs mmdc as mmdc.cmd (not mmdc.exe). CreateProcess does not perform
        // PATHEXT expansion, so we must go through cmd.exe /c to let the shell resolve the .cmd extension.
        var (mmdcExe, mmdcArgs) = OperatingSystem.IsWindows()
            ? ("cmd.exe", new[] { "/c", MermaidCliToolName, "-i", diagramFileName, "-o", imageFileName, "--scale", "4" })
            : (MermaidCliToolName, ["-i", diagramFileName, "-o", imageFileName, "--scale", "4"]);

        var mmdProcess = ProcessBuilder
            .For(mmdcExe)
            .WithNoWindow()
            .WithArguments(mmdcArgs)
            .WithErrorOutputHandler((sender, eventArgs) =>
            {
                if (eventArgs.Data is string message)
                {
                    Logger.WriteLine(ConsoleColor.Red, $"  {message}");
                }
            })
            .BuildProcessExecutor();

        _ = await mmdProcess
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        stopwatch.Stop();

        Logger.WriteLine(ConsoleColor.Green, $"Done ({FormatElapsed(stopwatch.Elapsed)})");
    }

    private static DiagramIrNode FindNode(DiagramIntermediateRepresentation diagramRepresentation, string alias)
    {
        foreach (var node in diagramRepresentation.Nodes)
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

    /// <summary>Mermaid node IDs may not contain dots - replace with underscores.</summary>
    private static string MermaidSafeAlias(string alias) => alias.Replace(".", "_");
}