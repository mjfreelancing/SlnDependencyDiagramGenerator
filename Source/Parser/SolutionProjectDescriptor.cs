namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents a project entry discovered from a solution file.</summary>
/// <param name="ProjectName">The project name without file extension.</param>
/// <param name="AbsolutePath">The absolute project path.</param>
internal readonly record struct SolutionProjectDescriptor(string ProjectName, string AbsolutePath);
