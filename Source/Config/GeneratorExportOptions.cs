namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Specifies export path and image format options.</summary>
public sealed class GeneratorExportOptions
{
    /// <summary>When <see langword="true"/>, clears sub-folders under <see cref="RootPath"/> for each target
    /// framework and the diagram-format sub-folders for configured renderer formats only.</summary>
    public bool ClearContents { get; set; }

    /// <summary>The relative or fully-qualified export root path for the generated diagram files and images.
    /// Sub-folders are created per target framework and then per configured diagram format.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>The diagram image formats to create. Can be empty, or one or more of "png", "svg", "pdf".</summary>
    public DiagramImageFormat[] ImageFormats { get; init; } = [];
}