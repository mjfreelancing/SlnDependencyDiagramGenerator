using AllOverIt.Patterns.Enumeration;
using System.Runtime.CompilerServices;

namespace SlnDependencyDiagramGenerator.Enumerations;

/// <summary>Generator-specific exit codes reserved for the core generator library.</summary>
public sealed class GeneratorExitCode : EnrichedEnum<GeneratorExitCode>
{
    /// <summary>Generator operation completed successfully.</summary>
    public static readonly GeneratorExitCode Success = new(0);

    /// <summary>The generation operation was canceled.</summary>
    public static readonly GeneratorExitCode OperationCanceled = new(1);

    /// <summary>Generator configuration validation failed.</summary>
    public static readonly GeneratorExitCode ValidationFailed = new(2);

    /// <summary>A required external export tool was unavailable.</summary>
    public static readonly GeneratorExitCode RequiredToolUnavailable = new(3);

    /// <summary>An unexpected generator failure occurred.</summary>
    public static readonly GeneratorExitCode UnhandledGeneratorFailure = new(999);

    /// <summary>Initializes a new instance of <see cref="GeneratorExitCode"/>.</summary>
    /// <param name="value">The numeric exit code value.</param>
    /// <param name="name">The enum entry name.</param>
    public GeneratorExitCode(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}