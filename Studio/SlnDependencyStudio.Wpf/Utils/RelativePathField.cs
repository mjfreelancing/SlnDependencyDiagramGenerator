namespace SlnDependencyStudio.Wpf.Utils;

/// <summary>Describes a single document-relative path that would be affected by saving the project to a new folder.</summary>
/// <param name="Label">A user-facing label for the field (e.g. "Solution path").</param>
/// <param name="Path">The stored relative path value.</param>
public sealed record RelativePathField(string Label, string Path);
