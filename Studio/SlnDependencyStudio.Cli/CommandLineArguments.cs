namespace SlnDependencyStudio.Cli;

/// <summary>Wraps the raw command-line arguments (excluding the executable name).</summary>
/// <param name="Args">The command-line arguments (excluding the executable name).</param>
internal sealed record CommandLineArguments(string[] Args);
