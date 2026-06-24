using AllOverIt.Patterns.Enumeration;
using System.Runtime.CompilerServices;

namespace SlnDependencyStudio.Cli.Enumerations;

/// <summary>CLI-specific exit codes reserved for command-line frontend behavior.</summary>
public sealed class StudioCliExitCode : EnrichedEnum<StudioCliExitCode>
{
    /// <summary>Command-line parsing failed.</summary>
    public static readonly StudioCliExitCode CommandLineParseFailed = new(1001);

    /// <summary>The config file path does not exist.</summary>
    public static readonly StudioCliExitCode ConfigFileNotFound = new(1002);

    /// <summary>The <c>validate</c> command failed.</summary>
    public static readonly StudioCliExitCode ValidateCommandFailed = new(1003);

    /// <summary>The <c>run</c> command failed.</summary>
    public static readonly StudioCliExitCode RunCommandFailed = new(1004);

    /// <summary>The pre-generation command failed and continue-on-failure is disabled.</summary>
    public static readonly StudioCliExitCode PreGenerationCommandFailed = new(1005);

    /// <summary>The diagram generator threw an error during <c>CreateDiagramsAsync</c>.</summary>
    public static readonly StudioCliExitCode DiagramGeneratorFailed = new(1006);

    /// <summary>An invalid regular expression was provided for project inclusion/exclusion.</summary>
    public static readonly StudioCliExitCode InvalidRegex = new(1007);

    /// <summary>An unexpected CLI failure occurred.</summary>
    public static readonly StudioCliExitCode UnhandledCliFailure = new(1999);

    /// <summary>Initializes a new instance of <see cref="StudioCliExitCode"/>.</summary>
    /// <param name="value">The numeric exit code value.</param>
    /// <param name="name">The enum entry name.</param>
    public StudioCliExitCode(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}
