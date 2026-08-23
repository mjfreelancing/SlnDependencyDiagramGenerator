namespace SlnDependencyDiagramGenerator.Config;

/// <summary>The image formats that can be created from the generated diagram files.</summary>
public enum DiagramImageFormat
{
    /// <summary>The SVG format. Supported by D2 and Mermaid.</summary>
    Svg,

    /// <summary>The PNG format. Supported by D2 and Mermaid.</summary>
    Png,

    /// <summary>The PDF format. Supported by D2 and Mermaid.</summary>
    Pdf
}