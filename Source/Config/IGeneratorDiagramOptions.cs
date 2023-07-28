namespace SlnDependencyDiagramGenerator.Config
{
    public interface IGeneratorDiagramOptions
    {
        // RGB fill color for explicit package references.
        public string PackageFill { get; }

        // RGB fill color for implicit (transitive) package references.
        public string TransitiveFill { get; }

        // The name (title) to use for the group of projects parsed.
        public string GroupName { get; }

        // The prefix to use in the D2 generated file to represent the group of projects parsed.
        // This prefix is not included in the diagram output, but it is required in the D2
        // file so the generated diagram can group the projects together.
        public string GroupNamePrefix { get; }
    }
}