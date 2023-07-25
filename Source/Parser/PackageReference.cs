using System.Collections.Generic;

namespace AllOverItDependencyDiagram.Parser
{
    internal sealed class PackageReference
    {
        public bool IsTransitive { get; }           // Implicit package referenced by an explicit package reference
        public int Depth { get; }                   // 0 for non-transitive, 1 or greater for transitive
        public string Name { get; init; }
        public string Version { get; init; }
        public IReadOnlyCollection<PackageReference> TransitiveReferences { get; init; }

        public PackageReference()
            : this(false, 0)
        {
        }

        public PackageReference(bool isTransitive, int depth)
        {
            IsTransitive = isTransitive;
            Depth = depth;
        }
    }
}