using System.Collections.Generic;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser.Resolvers;

/// <summary>Defines a strategy for loading projects from a solution file format.</summary>
internal interface ISolutionProjectResolver
{
    /// <summary>Gets the supported solution file extension, including leading dot.</summary>
    string Extension { get; }

    /// <summary>Loads all project entries from the provided solution file path.</summary>
    /// <param name="solutionFilePath">The full path to the solution file.</param>
    /// <returns>The projects discovered in the solution.</returns>
    Task<IReadOnlyList<SolutionProjectDescriptor>> GetProjectsAsync(string solutionFilePath);
}
