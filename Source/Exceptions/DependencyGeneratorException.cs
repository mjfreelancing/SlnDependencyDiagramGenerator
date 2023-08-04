using System;

namespace SlnDependencyDiagramGenerator.Exceptions
{
    /// <summary>The exception type raised when an error occurs while processing the dependency diagram generator.</summary>
    public sealed class DependencyGeneratorException : Exception
    {
        /// <summary>Constructor.</summary>
        /// <param name="message">The error message.</param>
        public DependencyGeneratorException(string message)
            : base(message)
        {
        }
    }
}