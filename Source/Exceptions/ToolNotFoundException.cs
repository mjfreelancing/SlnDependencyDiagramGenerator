using System;

namespace SlnDependencyDiagramGenerator.Exceptions;

/// <summary>Exception thrown when a required external diagram tool (d2 or mmdc) is not available.</summary>
public sealed class ToolNotFoundException : DependencyGeneratorException
{
    /// <summary>Initializes a new tool-not-found exception.</summary>
    /// <param name="message">The error message describing the missing tool(s).</param>
    public ToolNotFoundException(string message)
        : base(message)
    {
    }
}
