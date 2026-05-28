using SlnDependencyDiagramGenerator.Config;
using System.Collections.Generic;
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
        var sb = new StringBuilder();

        // Mermaid style lines must be collected separately (they follow all node/edge declarations).
        var styles = new List<string>();

        // Tracks emitted node/edge lines so recursive traversal does not duplicate declarations.
        var emitted = new HashSet<string>();

        // Tracks style state keys so each node gets one effective style line.
        var styledNodes = new HashSet<string>();

        sb.AppendLine($"flowchart {MermaidDirection()}");
        sb.AppendLine($"  subgraph {Options.GroupNameAlias}[\"{Options.GroupName}\"]");
        sb.AppendLine($"    direction {MermaidDirection()}");

        foreach (var project in model.Projects)
        {
            EmitProject(project, model, sb, styles, emitted, styledNodes);
        }

        sb.AppendLine("  end");

        foreach (var style in styles)
        {
            sb.AppendLine(style);
        }

        return sb.ToString();
    }

    /// <summary>Emits Mermaid lines for a project and its direct references.</summary>
    /// <param name="project">The project to emit.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="sb">The output builder for node/edge lines.</param>
    /// <param name="styles">The collected style lines appended after graph lines.</param>
    /// <param name="emitted">Set of already-emitted node/edge lines for de-duplication.</param>
    /// <param name="styledNodes">Set of style-state keys used to control style precedence.</param>
    private void EmitProject(ProjectNode project, DependencyGraphModel model,
        StringBuilder sb, List<string> styles, HashSet<string> emitted, HashSet<string> styledNodes)
    {
        var projAlias = MermaidSafeAlias(ProjectAlias(project.Name));
        EmitNode(sb, emitted, projAlias, project.Name);

        // Framework references
        foreach (var fw in project.FrameworkReferences)
        {
            var fwAlias = MermaidSafeAlias(Sanitise(fw.Name));

            EmitNode(sb, emitted, fwAlias, fw.Name);
            EmitEdge(sb, emitted, projAlias, fwAlias);

            AddStyle(styles, styledNodes, fwAlias, Options.FrameworkStyle.Fill, Options.FrameworkStyle.Opacity);
        }

        // Package references (recursive)
        foreach (var pkg in project.PackageReferences)
        {
            EmitPackage(pkg, projAlias, model, sb, styles, emitted, styledNodes);
        }

        // Project-to-project references
        foreach (var refName in project.ProjectReferences)
        {
            var refProjectName = GetProjectName(refName);
            var refAlias = MermaidSafeAlias(ProjectAlias(refProjectName));

            EmitNode(sb, emitted, refAlias, refProjectName);
            EmitEdge(sb, emitted, projAlias, refAlias);
            EmitProjectPackages(refProjectName, model, sb, styles, emitted, styledNodes);
        }
    }

    /// <summary>Recursively emits package and project-reference lines for a referenced project.</summary>
    /// <param name="projectName">The referenced project name.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="sb">The output builder for node/edge lines.</param>
    /// <param name="styles">The collected style lines appended after graph lines.</param>
    /// <param name="emitted">Set of already-emitted node/edge lines for de-duplication.</param>
    /// <param name="styledNodes">Set of style-state keys used to control style precedence.</param>
    private void EmitProjectPackages(string projectName, DependencyGraphModel model,
        StringBuilder sb, List<string> styles, HashSet<string> emitted, HashSet<string> styledNodes)
    {
        foreach (var project in model.Projects)
        {
            if (project.Name != projectName)
            {
                continue;
            }

            var projectAlias = MermaidSafeAlias(ProjectAlias(project.Name));

            foreach (var package in project.PackageReferences)
            {
                EmitPackage(package, projectAlias, model, sb, styles, emitted, styledNodes);
            }

            foreach (var projectReferenceName in project.ProjectReferences)
            {
                var referencedProjectName = GetProjectName(projectReferenceName);
                var referencedAlias = MermaidSafeAlias(ProjectAlias(referencedProjectName));

                EmitNode(sb, emitted, referencedAlias, referencedProjectName);
                EmitEdge(sb, emitted, projectAlias, referencedAlias);
                EmitProjectPackages(referencedProjectName, model, sb, styles, emitted, styledNodes);
            }

            break;
        }
    }

    /// <summary>Emits a package node, edge, style, and recursively emits transitive package dependencies.</summary>
    /// <param name="pkg">The package to emit.</param>
    /// <param name="parentAlias">The parent alias that the package edge should originate from.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="sb">The output builder for node/edge lines.</param>
    /// <param name="styles">The collected style lines appended after graph lines.</param>
    /// <param name="emitted">Set of already-emitted node/edge lines for de-duplication.</param>
    /// <param name="styledNodes">Set of style-state keys used to control style precedence.</param>
    private void EmitPackage(PackageNode pkg, string parentAlias, DependencyGraphModel model,
        StringBuilder sb, List<string> styles, HashSet<string> emitted, HashSet<string> styledNodes)
    {
        var pkgAlias = MermaidSafeAlias(PackageAlias(pkg, model));

        EmitNode(sb, emitted, pkgAlias, $"{pkg.Name}<br>v{pkg.Version}");
        EmitEdge(sb, emitted, parentAlias, pkgAlias);

        // Explicit style wins over transitive for the same alias.
        // styledNodes records explicit/styled keys so recursive visits do not re-apply stale styles.
        if (!pkg.IsTransitive || !styledNodes.Contains(pkgAlias + ":explicit"))
        {
            var fill = pkg.IsTransitive ? Options.TransitiveStyle.Fill : Options.PackageStyle.Fill;
            var opacity = pkg.IsTransitive ? Options.TransitiveStyle.Opacity : Options.PackageStyle.Opacity;

            if (!pkg.IsTransitive)
            {
                // Remove any previously added transitive style for this node
                styles.RemoveAll(style => style.StartsWith($"  style {pkgAlias} "));
                styledNodes.Add(pkgAlias + ":explicit");
            }

            if (!styledNodes.Contains(pkgAlias + ":explicit") || !pkg.IsTransitive)
            {
                AddStyle(styles, styledNodes, pkgAlias, fill, opacity);
            }
        }

        foreach (var child in pkg.TransitiveReferences)
        {
            EmitPackage(child, pkgAlias, model, sb, styles, emitted, styledNodes);
        }
    }

    /// <summary>Emits a node declaration if it has not already been emitted.</summary>
    private static void EmitNode(StringBuilder sb, HashSet<string> emitted, string alias, string label)
    {
        var line = $"    {alias}[\"{label}\"]";

        if (emitted.Add(line))
        {
            sb.AppendLine(line);
        }
    }

    /// <summary>Emits an edge declaration if it has not already been emitted.</summary>
    private static void EmitEdge(StringBuilder sb, HashSet<string> emitted, string from, string to)
    {
        var line = $"  {from} --> {to}";

        if (emitted.Add(line))
        {
            sb.AppendLine(line);
        }
    }

    /// <summary>Adds a style declaration for a node alias if one has not already been registered.</summary>
    private static void AddStyle(List<string> styles, HashSet<string> styledNodes,
        string alias, string fill, double opacity)
    {
        var key = $"{alias}:styled";

        if (styledNodes.Add(key))
        {
            styles.Add($"  style {alias} fill:{fill},opacity:{opacity}");
        }
    }

    /// <summary>Mermaid node IDs may not contain dots — replace with underscores.</summary>
    private static string MermaidSafeAlias(string alias) => alias.Replace(".", "_");
}
