namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Takes a resolved <see cref="DependencyGraphModel"/> and returns the text content of the diagram file.</summary>
internal interface IDiagramRenderer
{
    /// <summary>The file extension for this renderer's output (without the leading dot), e.g. "d2" or "mmd".</summary>
    string FileExtension { get; }

    /// <summary>Renders the dependency graph and returns the file content as a string.</summary>
    /// <param name="model">The resolved dependency graph model.</param>
    string Render(DependencyGraphModel model);
}
