using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies export path and image format options.</summary>
    public sealed class GeneratorExportOptions
    {
        /// <summary>Clears the contents of the <see cref="Path"/> when true.</summary>
        public bool ClearContents { get; init; }

        /// <summary>The fully-qualified export path for the generated diagram files and images.</summary>
        public string Path { get; init; }

        /// <summary>The diagram image formats to create. Can be empty or one or more of "png", "svg", "pdf".</summary>
        public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; init; }
    }
}