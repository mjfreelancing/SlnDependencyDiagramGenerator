using AllOverIt.Patterns.Enumeration;
using System.Runtime.CompilerServices;

namespace SlnDependencyStudio.Shared.Enumerations;

/// <summary>CLI-specific exit codes reserved for command-line frontend behavior.</summary>
public sealed class StudioExitCode : EnrichedEnum<StudioExitCode>
{
    /// <summary>The pre-generation command was cancelled before or during execution.</summary>
    public static readonly StudioExitCode PreGenerationCommandCancelled = new(1);

    /// <summary>The pre-generation command did not complete within the configured timeout period.</summary>
    public static readonly StudioExitCode PreGenerationCommandTimeout = new(2);

    /// <summary>An unexpected error occurred while executing the pre-generation command.</summary>
    public static readonly StudioExitCode PreGenerationUnexpectedError = new(999);

    /// <summary>Initializes a new instance of <see cref="StudioExitCode"/>.</summary>
    /// <param name="value">The numeric exit code value.</param>
    /// <param name="name">The enum entry name.</param>
    public StudioExitCode(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}
