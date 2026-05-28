using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Implemented by every diagram renderer. Takes a resolved <see cref="DependencyGraphModel"/>
/// and returns the text content of the diagram file.</summary>
internal interface IDiagramRenderer
{
    /// <summary>Renders the dependency graph and returns the file content as a string.</summary>
    string Render(DependencyGraphModel model);

    /// <summary>The file extension for this renderer's output (without the leading dot), e.g. "d2" or "mmd".</summary>
    string FileExtension { get; }
}

/// <summary>Shared helpers used by all renderer implementations.</summary>
internal abstract class DiagramRendererBase : IDiagramRenderer
{
    /// <summary>The diagram options used while rendering output.</summary>
    protected readonly GeneratorDiagramOptions Options;

    /// <summary>Initializes a new renderer base instance.</summary>
    /// <param name="options">The diagram options.</param>
    protected DiagramRendererBase(GeneratorDiagramOptions options)
    {
        Options = options;
    }

    /// <inheritdoc />
    public abstract string Render(DependencyGraphModel model);

    /// <inheritdoc />
    public abstract string FileExtension { get; }

    /// <summary>Returns the D2 / Mermaid node ID for a project name:
    /// dots replaced with hyphens, lower-cased, prefixed with the group alias.</summary>
    protected string ProjectAlias(string projectName)
        => $"{Options.GroupNameAlias}.{Sanitise(projectName)}";

    /// <summary>Returns a safe node ID with no group prefix — used for framework nodes and standalone IDs.</summary>
    protected static string Sanitise(string name)
        => name.Replace(".", "-").ToLowerInvariant();

    /// <summary>Returns the node ID for a package, handling the multi-version grouping case.</summary>
    protected static string PackageAlias(PackageNode pkg, DependencyGraphModel model)
    {
        var baseAlias = $"{pkg.Name}_{pkg.Version}".Replace(".", "-").ToLowerInvariant();

        return model.PackagesWithMultipleVersions.TryGetValue(pkg.Name, out var groupId)
            ? $"{groupId}-group.{baseAlias}"
            : baseAlias;
    }

    /// <summary>Returns the Mermaid flowchart direction string for the configured direction.</summary>
    protected string MermaidDirection() => Options.Direction.ToString();

    /// <summary>Maps the configured direction to a D2 direction keyword.</summary>
    protected string D2Direction() => Options.Direction switch
    {
        GeneratorDiagramOptions.DiagramDirection.LR => "left",
        GeneratorDiagramOptions.DiagramDirection.RL => "right",
        GeneratorDiagramOptions.DiagramDirection.BT => "up",
        _ => "down"   // TB
    };

    /// <summary>Gets a project name from a project path.</summary>
    /// <param name="path">The project path.</param>
    /// <returns>The project name without file extension.</returns>
    protected static string GetProjectName(string path)
        => Path.GetFileNameWithoutExtension(path);
}
