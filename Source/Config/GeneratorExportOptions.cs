using AllOverItDependencyDiagram.Generator;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies export path and image format options.</summary>
    public sealed class GeneratorExportOptions
    {
        /// <summary>When <see langword="True"/>, clears the contents of the folder that combines <see cref="RootPath"/>
        /// and the target framework being processed.</summary>
        public bool ClearContents { get; set; }

        /// <summary>The relative or fully-qualified export root path for the generated diagram files and images.
        /// A sub-folder will be created for each target framework processed.</summary>
        public string RootPath { get; set; }

        /// <summary>The diagram image formats to create. Can be empty, or one or more of "png", "svg", "pdf".</summary>
        public IList<DiagramImageFormat> ImageFormats { get; init; } = [];
    }
}