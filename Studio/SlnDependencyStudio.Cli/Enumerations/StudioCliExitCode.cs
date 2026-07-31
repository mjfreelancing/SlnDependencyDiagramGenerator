namespace SlnDependencyStudio.Cli.Enumerations;

public enum StudioCliExitCode
{
    /// <summary>Command-line parsing failed.</summary>
    CommandLineParseFailed = 1001,

    /// <summary>The config file path does not exist or the file is malformed.</summary>
    CannotLoadConfigFile = 1002,

    /// <summary>The <c>validate</c> command failed.</summary>
    ValidateCommandFailed = 1003,

    /// <summary>The <c>run</c> command failed.</summary>
    RunCommandFailed = 1004,

    /// <summary>The pre-generation command failed and continue-on-failure is disabled.</summary>
    PreGenerationCommandFailed = 1005,

    /// <summary>The diagram generator threw an error during <c>CreateDiagramsAsync</c>.</summary>
    DiagramGeneratorFailed = 1006,

    /// <summary>A required external diagram tool (such as d2 or mmdc) was not found.</summary>
    DiagramToolNotFound = 1007,

    /// <summary>The solution restore (via <c>dotnet restore</c>) failed.</summary>
    DotNetRestoreFailed = 1008,

    /// <summary>An unexpected CLI failure occurred.</summary>
    UnhandledCliFailure = 1999
}
