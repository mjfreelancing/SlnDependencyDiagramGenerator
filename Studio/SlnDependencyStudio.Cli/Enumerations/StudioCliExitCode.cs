namespace SlnDependencyStudio.Cli.Enumerations;

/// <summary>Exit codes returned by the CLI to signal command outcomes.</summary>
public enum StudioCliExitCode
{
    /// <summary>Command-line parsing failed.</summary>
    CommandLineParseFailed = 1001,

    /// <summary>The project file path does not exist or the file is malformed.</summary>
    CannotLoadProjectFile = 1002,

    /// <summary>The <c>validate</c> command failed.</summary>
    ValidateCommandFailed = 1003,

    /// <summary>The <c>run</c> command failed.</summary>
    RunCommandFailed = 1004,

    /// <summary>The pre-generation command failed and continue-on-failure is disabled.</summary>
    PreGenerationCommandFailed = 1005,

    /// <summary>The post-generation command failed.</summary>
    PostGenerationCommandFailed = 1006,

    /// <summary>The diagram generator failed while creating the diagrams.</summary>
    DiagramGeneratorFailed = 1007,

    /// <summary>A required external diagram tool (such as d2 or mmdc) was not found.</summary>
    DiagramToolNotFound = 1008,

    /// <summary>The solution restore (via <c>dotnet restore</c>) failed.</summary>
    DotNetRestoreFailed = 1009,

    /// <summary>The diagram generator failed to export an image (via d2 or mmdc).</summary>
    DiagramImageExportFailed = 1010,

    /// <summary>The project assets could not be read (missing assets file or unsupported assets format).</summary>
    ProjectAssetsFailed = 1011,

    /// <summary>The project dependency graph is inconsistent (a referenced project was not found or a circular reference was detected).</summary>
    DependencyGraphFailed = 1012,

    /// <summary>The command was cancelled by the user (Ctrl+C/SIGTERM).</summary>
    UserCancelled = 1013,

    /// <summary>An operation was cancelled internally (not by a user shutdown request).</summary>
    OperationCancelled = 1014,

    /// <summary>An unexpected CLI failure occurred.</summary>
    UnhandledCliFailure = 1999
}
