namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies diagram options that determine how the diagram will be styled.</summary>
    public sealed class GeneratorDiagramOptions
    {
        /// <summary>Diagram direction options.</summary>
        public enum DiagramDirection
        {
            /// <summary>The direction flows towards left.</summary>
            Left,

            /// <summary>The direction flows towards right.</summary>
            Right,

            /// <summary>The direction flows towards up.</summary>
            Up,

            /// <summary>The direction flows towards down.</summary>
            Down
        }

        /// <summary>The fill style used for framework and package dependencies.</summary>
        public sealed class FillStyle
        {
            /// <summary>The CSS or RGB fill color.</summary>
            public string Fill { get; set; }

            /// <summary>The opacity. This should be a value between 0 and 1.</summary>
            public double Opacity { get; set; }
        }

        /// <summary>Specifies the direction the diagram flows towards.</summary>
        public DiagramDirection Direction { get; set; } = DiagramDirection.Left;

        /// <summary>The fill style to use for framework dependencies referenced by a project.</summary>
        public FillStyle FrameworkStyle { get; init; } = new();

        /// <summary>The fill style to use for explicit package dependencies referenced by a project.</summary>
        public FillStyle PackageStyle { get; init; } = new();

        /// <summary>The fill style to use for transitive (implicit) package dependencies referenced by a project.</summary>
        public FillStyle TransitiveStyle { get; init; } = new();

        /// <summary>The name (title) to use for the group of projects parsed.</summary>
        public string GroupName { get; set; }

        /// <summary>The alias to use in the D2 generated file to represent the group of projects parsed.
        /// This prefix is not included in the diagram image output, but it is required in the D2
        /// file so the generated diagram can visually group the projects together.</summary>
        public string GroupNameAlias { get; set; }
    }
}