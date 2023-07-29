using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorExportOptions
    {
        public bool ClearContents { get; init; }
        public string Path { get; init; }
        public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; init; }
    }
}