using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public interface IGeneratorExportOptions
    {
        // Clears the contents of the 'Path' when true
        bool ClearContents { get; }

        // The fully-qualified export path for the generated diagram files and images.
        string Path { get; }

        // Diagram image formats to create. Can be empty or one or more of "png", "svg", "pdf".
        IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; }
    }
}