using System.Diagnostics;
using System.Text;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Support;

/// <summary>
/// Restores the fixture solutions so each fixture project has its <c>obj/project.assets.json</c> file.
/// </summary>
/// <remarks>
/// The fixture projects under <c>Fixtures/</c> are real SDK-style projects consumed by the integration
/// tests, but they are not part of the main solution and their <c>obj/</c> output is git-ignored.
/// A fresh clone therefore has no restored assets, which previously forced a manual build of the
/// fixtures before the tests could run. Restoring here keeps the tests self-sufficient and ensures
/// the assets stay in sync with any fixture changes.
/// </remarks>
internal static class FixtureRestorer
{
    private static readonly string[] SupportedSolutionExtensions = [".sln", ".slnx"];

    /// <summary>Restores every fixture solution, throwing when any restore fails.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no fixture solutions are found or a restore fails.</exception>
    public static async Task RestoreAllAsync()
    {
        var fixturesDirectory = FixtureLocator.GetFixturesDirectory();

        var solutionFiles = Directory.EnumerateFiles(fixturesDirectory, "*", SearchOption.AllDirectories)
            .Where(filePath => SupportedSolutionExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
            .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (solutionFiles.Length == 0)
        {
            throw new InvalidOperationException($"No fixture solutions were found under '{fixturesDirectory}'.");
        }

        foreach (var solutionFile in solutionFiles)
        {
            await RestoreSolutionAsync(solutionFile).ConfigureAwait(false);
        }
    }

    /// <summary>Runs <c>dotnet restore</c> for a single solution file.</summary>
    /// <param name="solutionFilePath">The absolute solution file path.</param>
    /// <exception cref="InvalidOperationException">Thrown when the restore fails or the process cannot be started.</exception>
    private static async Task RestoreSolutionAsync(string solutionFilePath)
    {
        var output = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(solutionFilePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start 'dotnet restore' for '{solutionFilePath}'.");

        // Read both output streams concurrently so a large restore cannot deadlock on a full pipe buffer.
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().ConfigureAwait(false);

        output.Append("Standard Output: ");
        output.Append(await standardOutputTask.ConfigureAwait(false));
        output.AppendLine();
        output.Append("Standard Error: ");
        output.Append(await standardErrorTask.ConfigureAwait(false));

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'dotnet restore' failed for '{solutionFilePath}' (exit code {process.ExitCode}).{Environment.NewLine}{output}");
        }
    }
}
