using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser.Resolvers;

/// <summary>Loads project entries from legacy <c>.sln</c> files.</summary>
internal sealed class SlnSolutionProjectResolver : ISolutionProjectResolver
{
    /// <inheritdoc />
    public string Extension => ".sln";

    /// <inheritdoc />
    public Task<IReadOnlyList<SolutionProjectDescriptor>> GetProjectsAsync(string solutionFilePath)
    {
        var solutionFile = SolutionFile.Parse(solutionFilePath);

        IReadOnlyList<SolutionProjectDescriptor> projects = [.. solutionFile.ProjectsInOrder
            .Where(project =>
                project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                project.ProjectType == SolutionProjectType.WebProject)
            .Select(project => new SolutionProjectDescriptor(project.ProjectName, project.AbsolutePath))];

        return Task.FromResult(projects);
    }
}
