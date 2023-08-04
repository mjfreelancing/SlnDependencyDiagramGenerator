using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies export path and image format options.</summary>
    public sealed class GeneratorExportOptions
    {
        public bool ClearContents { get; init; }
        public string Path { get; init; }
        public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; init; }
    }
}