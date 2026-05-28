using SlnDependencyDiagramGenerator.Config;
using System.Collections.Generic;
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
        var sb = new StringBuilder();

        // Tracks emitted D2 lines (nodes, edges, style statements) and naturally de-duplicates repeats.
        var set = new HashSet<string>();

        sb.AppendLine($"direction: {D2Direction()}");
        sb.AppendLine();
        sb.AppendLine($"{Options.GroupNameAlias}: {Options.GroupName}");

        foreach (var project in model.Projects)
        {
            EmitProject(project, model, set);
        }

        foreach (var line in set)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>Emits the D2 node, edges, and styles for a project and its direct references.</summary>
    /// <param name="project">The project to emit.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="set">The de-duplicated set of emitted D2 lines.</param>
    private void EmitProject(ProjectNode project, DependencyGraphModel model, HashSet<string> set)
    {
        var projAlias = ProjectAlias(project.Name);
        set.Add($"{projAlias}: {project.Name}");

        // Framework references
        foreach (var fw in project.FrameworkReferences)
        {
            var fwAlias = Sanitise(fw.Name);

            set.Add($"{fwAlias} <- {projAlias}");
            set.Add($"{fwAlias}.style.fill: \"{Options.FrameworkStyle.Fill}\"");
            set.Add($"{fwAlias}.style.opacity: {Options.FrameworkStyle.Opacity}");
        }

        // Package references (recursive)
        foreach (var pkg in project.PackageReferences)
        {
            EmitPackage(pkg, projAlias, model, set);
        }

        // Project-to-project references
        foreach (var refName in project.ProjectReferences)
        {
            var refAlias = ProjectAlias(GetProjectName(refName));

            set.Add($"{refAlias}: {GetProjectName(refName)}");
            set.Add($"{refAlias} <- {projAlias}");

            EmitProjectPackages(refName, model, set);
        }
    }

    /// <summary>Recursively emits package and project-reference lines for a referenced project.</summary>
    /// <param name="projectName">The referenced project path or name.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="set">The de-duplicated set of emitted D2 lines.</param>
    private void EmitProjectPackages(string projectName, DependencyGraphModel model, HashSet<string> set)
    {
        // Find the referenced project in the model and emit its packages/references recursively
        var referencedProjectName = GetProjectName(projectName);

        foreach (var project in model.Projects)
        {
            if (project.Name != referencedProjectName)
            {
                continue;
            }

            var projectAlias = ProjectAlias(project.Name);

            foreach (var package in project.PackageReferences)
            {
                EmitPackage(package, projectAlias, model, set);
            }

            foreach (var projectReferenceName in project.ProjectReferences)
            {
                var referencedAlias = ProjectAlias(GetProjectName(projectReferenceName));

                set.Add($"{referencedAlias}: {GetProjectName(projectReferenceName)}");
                set.Add($"{referencedAlias} <- {projectAlias}");

                EmitProjectPackages(projectReferenceName, model, set);
            }

            break;
        }
    }

    /// <summary>Emits a package node, style lines, and parent edge, then recurses into transitive packages.</summary>
    /// <param name="pkg">The package node to emit.</param>
    /// <param name="parentAlias">The parent node alias that the package depends from.</param>
    /// <param name="model">The complete dependency graph model.</param>
    /// <param name="set">The de-duplicated set of emitted D2 lines.</param>
    private void EmitPackage(PackageNode pkg, string parentAlias, DependencyGraphModel model, HashSet<string> set)
    {
        var pkgAlias = PackageAlias(pkg, model);

        // Emit multi-version group container if needed. The set prevents duplicates when encountered repeatedly.
        if (model.PackagesWithMultipleVersions.TryGetValue(pkg.Name, out var groupId))
        {
            set.Add($"{groupId}-group: \"\"");
        }

        set.Add($"{pkgAlias}: {pkg.Name}\\nv{pkg.Version}");

        // Style precedence is enforced via set membership/removal:
        // explicit style replaces any prior transitive style for the same node alias.
        var transitiveStyleFill = $"{pkgAlias}.style.fill: \"{Options.TransitiveStyle.Fill}\"";
        var packageStyleFill = $"{pkgAlias}.style.fill: \"{Options.PackageStyle.Fill}\"";

        if (pkg.IsTransitive)
        {
            if (!set.Contains(packageStyleFill))
            {
                set.Add(transitiveStyleFill);
                set.Add($"{pkgAlias}.style.opacity: {Options.TransitiveStyle.Opacity}");
            }
        }
        else
        {
            if (set.Remove(transitiveStyleFill))
            {
                set.Remove($"{pkgAlias}.style.opacity: {Options.TransitiveStyle.Opacity}");
            }

            set.Add(packageStyleFill);
            set.Add($"{pkgAlias}.style.opacity: {Options.PackageStyle.Opacity}");
        }

        set.Add($"{pkgAlias} <- {parentAlias}");

        foreach (var child in pkg.TransitiveReferences)
        {
            EmitPackage(child, pkgAlias, model, set);
        }
    }
}
