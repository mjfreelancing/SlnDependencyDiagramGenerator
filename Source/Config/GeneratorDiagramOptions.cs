namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorDiagramOptions
    {
        public sealed class FillStyle
        {
            public string Fill { get; init; }
            public double Opacity { get; init; }
        }

        public FillStyle FrameworkStyle { get; init; } = new();
        public FillStyle PackageStyle { get; init; } = new();
        public FillStyle TransitiveStyle { get; init; } = new();
        public string GroupName { get; init; }
        public string GroupNamePrefix { get; init; }
    }
}