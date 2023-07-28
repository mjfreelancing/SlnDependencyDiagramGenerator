namespace SlnDependencyDiagramGenerator.Config
{
    public class GeneratorDiagramOptions : IGeneratorDiagramOptions
    {
        public string PackageFill { get; set; }
        public string TransitiveFill { get; set; }
        public string GroupName { get; set; }
        public string GroupNamePrefix { get; set; }
    }
}