using SlnDependencyStudio.Wpf.Utils;

namespace SlnDependencyStudio.Wpf.Models;

/// <summary>Describes the document-relative paths that would be affected by saving the project to a new folder.</summary>
/// <param name="RelativePaths">The affected path fields.</param>
public sealed record RelativePathChangeInfo(IReadOnlyList<RelativePathField> RelativePaths);
