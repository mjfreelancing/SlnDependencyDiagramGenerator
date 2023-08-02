namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies diagram options that determine how the diagram will be styled.</summary>
    public class GeneratorDiagramOptions
    {
        public enum DiagramDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        public sealed class FillStyle
        {
            public string Fill { get; init; }
            public double Opacity { get; init; }
        }

        public DiagramDirection Direction { get; init; } = DiagramDirection.Left;
        public FillStyle FrameworkStyle { get; init; } = new();
        public FillStyle PackageStyle { get; init; } = new();
        public FillStyle TransitiveStyle { get; init; } = new();
        public string GroupName { get; init; }
        public string GroupNamePrefix { get; init; }
    }
}