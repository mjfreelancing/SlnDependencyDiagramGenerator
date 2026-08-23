using AllOverIt.Extensions;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser.Resolvers;

/// <summary>Loads project entries from modern <c>.slnx</c> files.</summary>
internal sealed class SlnxSolutionProjectResolver : ISolutionProjectResolver
{
    /// <inheritdoc />
    public string Extension => ".slnx";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SolutionProjectDescriptor>> GetProjectsAsync(string solutionFilePath, CancellationToken cancellationToken)
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath)
            ?? throw new InvalidOperationException($"No solution serializer is available for path '{solutionFilePath}'.");

        var solutionModel = await serializer.OpenAsync(solutionFilePath, cancellationToken).ConfigureAwait(false);
        var solutionDirectory = Path.GetDirectoryName(solutionFilePath) ?? string.Empty;

        return [.. solutionModel.SolutionProjects
            .Where(solutionProject =>
                !solutionProject.Extension.IsNullOrEmpty() &&
                solutionProject.Extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase))
            .Select(solutionProject => new SolutionProjectDescriptor(
                Path.GetFileNameWithoutExtension(solutionProject.FilePath),
                Path.GetFullPath(Path.Combine(solutionDirectory, solutionProject.FilePath))))];
    }
}
