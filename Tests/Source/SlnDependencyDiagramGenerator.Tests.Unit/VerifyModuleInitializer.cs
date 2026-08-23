using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VerifyTests;
using VerifyXunit;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

/// <summary>Initializes Verify configuration for unit tests.</summary>
internal static partial class VerifyModuleInitializer
{
    [GeneratedRegex(@"[A-Za-z]:\\[^\r\n\""']+", RegexOptions.Compiled)]
    private static partial Regex CreateAbsolutePathRegex();

    private static readonly Regex AbsolutePathRegex = CreateAbsolutePathRegex();

    /// <summary>Runs once when the test assembly is loaded.</summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        Verifier.UseProjectRelativeDirectory("Snapshots");

        VerifierSettings.AddScrubber(builder =>
        {
            // Normalize absolute Windows paths to keep snapshots deterministic across machines and CI agents.
            var normalized = AbsolutePathRegex.Replace(builder.ToString(), "{ABSOLUTE_PATH}");

            builder.Clear();
            builder.Append(normalized);
        });
    }
}