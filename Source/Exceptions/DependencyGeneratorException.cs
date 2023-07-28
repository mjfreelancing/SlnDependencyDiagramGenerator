using System;

namespace SlnDependencyDiagramGenerator.Exceptions
{
    public sealed class DependencyGeneratorException : Exception
    {
        public DependencyGeneratorException(string message)
            : base(message)
        {
        }
    }
}