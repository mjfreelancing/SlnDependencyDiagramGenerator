using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Renderers.D2;

/// <summary>Renders a <see cref="DependencyGraphModel"/> as a D2 diagram file.</summary>
internal sealed class D2DiagramRenderer : DiagramRendererBase
{
    private const string D2ToolName = "d2";

    /// <inheritdoc />
    public override string FileExtension => "d2";

    /// <summary>Initializes a new D2 diagram renderer.</summary>
    /// <param name="options">The diagram options.</param>
    /// <param name="toolPathResolver">Resolves effective tool paths for external CLI invocation.</param>
    /// <param name="logger">A logger for progress and diagnostics.</param>
    public D2DiagramRenderer(GeneratorDiagramOptions options, IToolPathResolver toolPathResolver, ILogger<D2DiagramRenderer> logger)
        : base(options, toolPathResolver, logger)
    {
    }

    /// <inheritdoc />
    public override string Render(DependencyGraphModel model)
    {
        // Build the shared, renderer-neutral graph representation.
        var diagramRepresentation = BuildIntermediateRepresentation(model);
        var sb = new StringBuilder();

        sb.AppendLine($"direction: {GetDirection()}");
        sb.AppendLine();

        if (Options.Grouping.Enabled)
        {
            sb.AppendLine($"{Options.GroupNameAlias}: {Options.GroupName}");
        }

        foreach (var group in diagramRepresentation.Groups)
        {
            // D2 package-group container for multi-version package sets.
            sb.AppendLine($"{group.Alias}: \"\"");
        }

        foreach (var node in diagramRepresentation.Nodes)
        {
            var label = node.Version is null
                ? node.Label
                : $"{node.Label}\\nv{node.Version}";

            sb.AppendLine($"{node.Alias}: {label}");
        }

        foreach (var edge in diagramRepresentation.Edges)
        {
            sb.AppendLine($"{edge.ToAlias} <- {edge.FromAlias}");
        }

        foreach (var style in diagramRepresentation.Styles)
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

            foreach (var group in diagramRepresentation.Groups)
            {
                sb.AppendLine($"{group.Alias}.style.fill: \"{groupFill}\"");
                sb.AppendLine($"{group.Alias}.style.opacity: {groupOpacity}");
            }
        }

        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override string GetDirection()
    {
        return Options.Direction switch
        {
            GeneratorDiagramOptions.DiagramDirection.LR => "left",
            GeneratorDiagramOptions.DiagramDirection.RL => "right",
            GeneratorDiagramOptions.DiagramDirection.BT => "up",
            GeneratorDiagramOptions.DiagramDirection.TB => "down",
            _ => throw new ArgumentOutOfRangeException(nameof(Options.Direction))
        };
    }

    /// <inheritdoc />
    protected override async Task ExportImageFileAsync(string diagramFileName, DiagramImageFormat format, CancellationToken cancellationToken)
    {
        var imageFileName = Path.ChangeExtension(diagramFileName, format.ToString().ToLowerInvariant());
        Logger.LogInformation("  Exporting {Format}: {FileName}", format, Path.GetFileName(imageFileName));

        var d2Path = ToolPathResolver.GetEffectivePath(DiagramFormat.D2);

        var stopwatch = Stopwatch.StartNew();

        // D2 sends all output to stderr - "err:" lines are errors, everything else is info/success.
        var d2Process = ProcessBuilder
            .For(d2Path)
            .WithNoWindow()
            .WithArguments("-l", "elk", diagramFileName, imageFileName)
            .WithStandardOutputHandler((sender, eventArgs) =>
            {
                // D2 doesn't output anything via StdOut, but keep this here in case that changes in the future
                if (eventArgs.Data is string message)
                {
                    Logger.LogInformation("  {D2Message}", message);
                }
            })
            .WithErrorOutputHandler((sender, eventArgs) =>
            {
                // D2 emits error and non-error messages via StdErr
                if (eventArgs.Data is string message)
                {
                    if (message.StartsWith("err:", true, CultureInfo.InvariantCulture))
                    {
                        Logger.LogError("  {D2Message}", message);
                    }
                    else
                    {
                        Logger.LogInformation("  {D2Message}", message);
                    }
                }
            })
            .BuildProcessExecutor();

        var result = await d2Process.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        AssertImageExportSucceeded(result, D2ToolName, diagramFileName, imageFileName);

        stopwatch.Stop();

        var elapsed = FormatElapsed(stopwatch.Elapsed);
        Logger.LogDebug("  Export complete ({Elapsed})", elapsed);
    }

    private (string Fill, double Opacity) GetStyleValues(DiagramIrStyleRole styleRole)
    {
        return styleRole switch
        {
            DiagramIrStyleRole.Framework => (Options.FrameworkStyle.Fill, Options.FrameworkStyle.Opacity),
            DiagramIrStyleRole.PackageExplicit => (Options.PackageStyle.Fill, Options.PackageStyle.Opacity),
            DiagramIrStyleRole.PackageTransitive => (Options.TransitiveStyle.Fill, Options.TransitiveStyle.Opacity),
            _ => throw new ArgumentOutOfRangeException(nameof(styleRole))
        };
    }

    private (string Fill, double Opacity) GetGroupStyleValues()
    {
        return (Options.Grouping.BackgroundStyle.Fill, Options.Grouping.BackgroundStyle.Opacity);
    }
}