using AllOverIt.Assertion;
using AllOverIt.Logging;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;
using SlnDependencyDiagramGenerator.Generator.Nodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Renderers;

/// <summary>Shared helpers used by all renderer implementations.</summary>
internal abstract class DiagramRendererBase : IDiagramRenderer
{
    /// <summary>The diagram options used while rendering output.</summary>
    protected readonly GeneratorDiagramOptions Options;

    /// <summary>The logger used for progress and diagnostics.</summary>
    protected readonly IColorConsoleLogger Logger;

    /// <inheritdoc />
    public abstract string FileExtension { get; }

    /// <summary>Initializes a new renderer base instance.</summary>
    /// <param name="options">The diagram options.</param>
    /// <param name="logger">A logger for progress and diagnostics.</param>
    protected DiagramRendererBase(GeneratorDiagramOptions options, IColorConsoleLogger logger)
    {
        Options = options.WhenNotNull();
        Logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public abstract string Render(DependencyGraphModel model);

    /// <inheritdoc />
    public virtual Task ValidateRequiredToolsAsync(bool imageExportEnabled) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task CreateDiagramArtifactsAsync(string targetFramework, string exportPath, string projectScope,
        DependencyGraphModel model, DiagramImageFormat[] imageFormats)
    {
        var content = Render(model);
        var baseName = GetDiagramAliasId(projectScope, includeProjectGroupPrefix: false);
        var rendererExportPath = Path.Combine(exportPath, FileExtension);

        Directory.CreateDirectory(rendererExportPath);

        var fileName = Path.Combine(rendererExportPath, $"{baseName}.{FileExtension}");

        Logger.Write($"{{forecolor:white}}Creating {{forecolor:yellow}}'{targetFramework}'{{forecolor:white}} diagram: ")
              .Write(ConsoleColor.Yellow, Path.GetFileName(fileName))
              .Write("{forecolor:white}...");

        await File.WriteAllTextAsync(fileName, content).ConfigureAwait(false);

        Logger.WriteLine("{forecolor:green}Done");

        foreach (var format in imageFormats)
        {
            await ExportImageFileAsync(fileName, format).ConfigureAwait(false);
        }
    }

    /// <summary>Exports an image for the generated diagram text file.</summary>
    /// <param name="diagramFileName">The source diagram file path.</param>
    /// <param name="format">The desired image format.</param>
    protected abstract Task ExportImageFileAsync(string diagramFileName, DiagramImageFormat format);

    /// <summary>
    /// Throws when a required external tool is not available on PATH.
    /// </summary>
    /// <param name="toolName">The command/tool name to check.</param>
    /// <param name="missingToolMessage">The error message for a missing tool.</param>
    protected static async Task EnsureToolAvailableAsync(string toolName, string missingToolMessage)
    {
        if (!await IsToolAvailableAsync(toolName).ConfigureAwait(false))
        {
            throw new DependencyGeneratorException(missingToolMessage);
        }
    }

    /// <summary>Returns elapsed time text with two decimal places in seconds.</summary>
    /// <param name="elapsed">The elapsed duration.</param>
    protected static string FormatElapsed(TimeSpan elapsed) => $"{elapsed.TotalSeconds:0.00}s";

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
    protected static string Sanitise(string name) => name.Replace(".", "-").ToLowerInvariant();

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

    /// <summary>Returns the renderer-specific direction keyword for the configured direction.</summary>
    protected abstract string GetDirection();

    /// <summary>Extracts the project name from a project path.</summary>
    /// <param name="path">The project path.</param>
    /// <returns>The project name without file extension.</returns>
    protected static string GetProjectName(string path) => Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// Builds a renderer-neutral diagram representation from the resolved dependency graph model.
    /// </summary>
    /// <remarks>
    /// This shared traversal is the consolidation point for all renderers.
    /// Every renderer gets identical nodes/edges/group semantics and only differs
    /// in how it serializes the diagram representation to syntax.
    /// </remarks>
    protected DiagramIntermediateRepresentation BuildIntermediateRepresentation(DependencyGraphModel model)
    {
        var diagramRepresentation = new DiagramIntermediateRepresentation();
        var projectsByName = model.Projects.ToDictionary(project => project.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var project in model.Projects)
        {
            EmitProjectToDiagramRepresentation(project, model, projectsByName, diagramRepresentation);
        }

        return diagramRepresentation;
    }

    private static async Task<bool> IsToolAvailableAsync(string toolName)
    {
        var locator = OperatingSystem.IsWindows() ? "where" : "which";

        try
        {
            using var executor = ProcessBuilder
                .For(locator)
                .WithNoWindow()
                .WithArguments(toolName)
                .BuildProcessExecutor();

            var result = await executor.ExecuteBufferedAsync().ConfigureAwait(false);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string GetDiagramAliasId(string alias, bool includeProjectGroupPrefix)
    {
        alias = alias.Replace(".", "-").ToLowerInvariant();

        return includeProjectGroupPrefix
            ? $"{Options.GroupNameAlias}.{alias}"
            : alias;
    }

    private void EmitProjectToDiagramRepresentation(ProjectNode project, DependencyGraphModel model,
        IDictionary<string, ProjectNode> projectsByName, DiagramIntermediateRepresentation diagramRepresentation)
    {
        var projectAlias = ProjectAlias(project.Name);

        diagramRepresentation.AddNode(projectAlias, project.Name);

        foreach (var framework in project.FrameworkReferences)
        {
            var frameworkAlias = Sanitise(framework.Name);

            diagramRepresentation.AddNode(frameworkAlias, framework.Name);
            diagramRepresentation.AddEdge(projectAlias, frameworkAlias);
            diagramRepresentation.SetStyleRole(frameworkAlias, DiagramIrStyleRole.Framework, allowOverride: true);
        }

        foreach (var package in project.PackageReferences)
        {
            EmitPackageToDiagramRepresentation(package, projectAlias, model, diagramRepresentation);
        }

        foreach (var projectReferenceName in project.ProjectReferences)
        {
            var referencedProjectName = GetProjectName(projectReferenceName);
            var referencedAlias = ProjectAlias(referencedProjectName);

            diagramRepresentation.AddNode(referencedAlias, referencedProjectName);
            diagramRepresentation.AddEdge(projectAlias, referencedAlias);

            EmitProjectPackagesToDiagramRepresentation(referencedProjectName, model, projectsByName, diagramRepresentation);
        }
    }

    private void EmitProjectPackagesToDiagramRepresentation(string projectName, DependencyGraphModel model,
        IDictionary<string, ProjectNode> projectsByName, DiagramIntermediateRepresentation diagramRepresentation)
    {
        if (!projectsByName.TryGetValue(projectName, out var project))
        {
            return;
        }

        var projectAlias = ProjectAlias(project.Name);

        foreach (var package in project.PackageReferences)
        {
            EmitPackageToDiagramRepresentation(package, projectAlias, model, diagramRepresentation);
        }

        foreach (var projectReferenceName in project.ProjectReferences)
        {
            var referencedProjectName = GetProjectName(projectReferenceName);
            var referencedAlias = ProjectAlias(referencedProjectName);

            diagramRepresentation.AddNode(referencedAlias, referencedProjectName);
            diagramRepresentation.AddEdge(projectAlias, referencedAlias);

            EmitProjectPackagesToDiagramRepresentation(referencedProjectName, model, projectsByName, diagramRepresentation);
        }
    }

    private void EmitPackageToDiagramRepresentation(PackageNode package, string parentAlias, DependencyGraphModel model,
        DiagramIntermediateRepresentation diagramRepresentation)
    {
        var groupingEnabled = Options.Grouping.Enabled;
        var packageAlias = PackageAlias(package, model, groupingEnabled);

        diagramRepresentation.AddNode(packageAlias, package.Name, package.Version);
        diagramRepresentation.AddEdge(parentAlias, packageAlias);

        if (groupingEnabled && model.PackagesWithMultipleVersions.TryGetValue(package.Name, out var groupId))
        {
            var groupAlias = $"{groupId}-group";

            diagramRepresentation.EnsureGroup(groupAlias, package.Name);
            diagramRepresentation.AddNodeToGroup(groupAlias, packageAlias);
        }

        if (package.IsTransitive)
        {
            // Do not override explicit style if the node was already seen as explicit.
            diagramRepresentation.SetStyleRole(packageAlias, DiagramIrStyleRole.PackageTransitive, allowOverride: false);
        }
        else
        {
            // Explicit style always wins when both explicit and transitive paths reach the same alias.
            diagramRepresentation.SetStyleRole(packageAlias, DiagramIrStyleRole.PackageExplicit, allowOverride: true);
        }

        foreach (var child in package.TransitiveReferences)
        {
            EmitPackageToDiagramRepresentation(child, packageAlias, model, diagramRepresentation);
        }
    }
}