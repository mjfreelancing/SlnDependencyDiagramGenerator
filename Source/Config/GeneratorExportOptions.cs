using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorExportOptions : IGeneratorExportOptions
    {
        public bool ClearContents { get; set; }
        public string Path { get; set; }
        public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; set; }
    }
}