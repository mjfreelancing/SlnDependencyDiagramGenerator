using System;

namespace SlnDependencyDiagramGenerator.Exceptions
{
    public sealed class DependencyDiagramGeneratorException : Exception
    {
        public DependencyDiagramGeneratorException(string message)
            : base(message)
        {
        }
    }
}