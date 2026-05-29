using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Shared helpers used by all renderer implementations.</summary>
internal abstract class DiagramRendererBase : IDiagramRenderer
{
    /// <summary>The diagram options used while rendering output.</summary>
    protected readonly GeneratorDiagramOptions Options;

    /// <inheritdoc />
    public abstract string FileExtension { get; }

    /// <summary>Initializes a new renderer base instance.</summary>
    /// <param name="options">The diagram options.</param>
    protected DiagramRendererBase(GeneratorDiagramOptions options)
    {
        Options = options;
    }

    /// <inheritdoc />
    public abstract string Render(DependencyGraphModel model);

    /// <summary>Returns the project node ID by sanitizing the project name (dots replaced with hyphens, lower-cased)
    /// and prefixing it with the group alias when grouping is enabled.</summary>
    protected string ProjectAlias(string projectName)
    {
        var sanitisedProjectName = Sanitise(projectName);

        return Options.Grouping.Enabled
            ? $"{Options.GroupNameAlias}.{sanitisedProjectName}"
            : sanitisedProjectName;
    }

    /// <summary>Returns a normalized node ID by replacing dots with hyphens and converting to lower-case; no group prefix is applied.</summary>
    protected static string Sanitise(string name)
        => name.Replace(".", "-").ToLowerInvariant();

    /// <summary>Returns the node ID for a package, optionally nesting it under a multi-version package group when grouping is enabled.</summary>
    protected static string PackageAlias(PackageNode pkg, DependencyGraphModel model, bool groupingEnabled)
    {
        var baseAlias = $"{pkg.Name}_{pkg.Version}".Replace(".", "-").ToLowerInvariant();

        if (!groupingEnabled)
        {
            return baseAlias;
        }

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

    /// <summary>Extracts the project name from a project path.</summary>
    /// <param name="path">The project path.</param>
    /// <returns>The project name without file extension.</returns>
    protected static string GetProjectName(string path)
        => Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// Builds a renderer-neutral IR from the resolved dependency graph model.
    /// </summary>
    /// <remarks>
    /// This shared traversal is the consolidation point for all renderers.
    /// Every renderer gets identical nodes/edges/group semantics and only differs
    /// in how it serializes the IR to syntax (D2, Mermaid, etc).
    /// </remarks>
    protected DiagramIntermediateRepresentation BuildIntermediateRepresentation(DependencyGraphModel model)
    {
        var ir = new DiagramIntermediateRepresentation();

        foreach (var project in model.Projects)
        {
            EmitProjectToIr(project, model, ir);
        }

        return ir;
    }

    private void EmitProjectToIr(ProjectNode project, DependencyGraphModel model, DiagramIntermediateRepresentation ir)
    {
        var projectAlias = ProjectAlias(project.Name);
        ir.AddNode(projectAlias, project.Name);

        foreach (var framework in project.FrameworkReferences)
        {
            var frameworkAlias = Sanitise(framework.Name);

            ir.AddNode(frameworkAlias, framework.Name);
            ir.AddEdge(projectAlias, frameworkAlias);
            ir.SetStyleRole(frameworkAlias, DiagramIrStyleRole.Framework, allowOverride: true);
        }

        foreach (var package in project.PackageReferences)
        {
            EmitPackageToIr(package, projectAlias, model, ir);
        }

        foreach (var projectReferenceName in project.ProjectReferences)
        {
            var referencedProjectName = GetProjectName(projectReferenceName);
            var referencedAlias = ProjectAlias(referencedProjectName);

            ir.AddNode(referencedAlias, referencedProjectName);
            ir.AddEdge(projectAlias, referencedAlias);

            EmitProjectPackagesToIr(referencedProjectName, model, ir);
        }
    }

    private void EmitProjectPackagesToIr(string projectName, DependencyGraphModel model, DiagramIntermediateRepresentation ir)
    {
        foreach (var project in model.Projects)
        {
            if (project.Name != projectName)
            {
                continue;
            }

            var projectAlias = ProjectAlias(project.Name);

            foreach (var package in project.PackageReferences)
            {
                EmitPackageToIr(package, projectAlias, model, ir);
            }

            foreach (var projectReferenceName in project.ProjectReferences)
            {
                var referencedProjectName = GetProjectName(projectReferenceName);
                var referencedAlias = ProjectAlias(referencedProjectName);

                ir.AddNode(referencedAlias, referencedProjectName);
                ir.AddEdge(projectAlias, referencedAlias);

                EmitProjectPackagesToIr(referencedProjectName, model, ir);
            }

            break;
        }
    }

    private void EmitPackageToIr(PackageNode package, string parentAlias, DependencyGraphModel model, DiagramIntermediateRepresentation ir)
    {
        var groupingEnabled = Options.Grouping.Enabled;
        var packageAlias = PackageAlias(package, model, groupingEnabled);
        ir.AddNode(packageAlias, package.Name, package.Version);
        ir.AddEdge(parentAlias, packageAlias);

        if (groupingEnabled && model.PackagesWithMultipleVersions.TryGetValue(package.Name, out var groupId))
        {
            var groupAlias = $"{groupId}-group";
            ir.EnsureGroup(groupAlias, package.Name);
            ir.AddNodeToGroup(groupAlias, packageAlias);
        }

        if (package.IsTransitive)
        {
            // Do not override explicit style if the node was already seen as explicit.
            ir.SetStyleRole(packageAlias, DiagramIrStyleRole.PackageTransitive, allowOverride: false);
        }
        else
        {
            // Explicit style always wins when both explicit and transitive paths reach the same alias.
            ir.SetStyleRole(packageAlias, DiagramIrStyleRole.PackageExplicit, allowOverride: true);
        }

        foreach (var child in package.TransitiveReferences)
        {
            EmitPackageToIr(child, packageAlias, model, ir);
        }
    }
}
