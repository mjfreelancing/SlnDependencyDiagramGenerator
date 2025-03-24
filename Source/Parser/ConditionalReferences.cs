using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Parser
{
    internal sealed class ConditionalReferences
    {
        public string Condition { get; init; }
        public IReadOnlyCollection<ProjectReference> ProjectReferences { get; init; }
        public IReadOnlyCollection<FrameworkReference> FrameworkReferences { get; init; }
        public IReadOnlyCollection<PackageReference> PackageReferences { get; init; }
    }
}