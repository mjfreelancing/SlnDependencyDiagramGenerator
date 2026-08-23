using AllOverIt.Extensions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Enumerations;
using System.IO;

namespace SlnDependencyStudio.Wpf.Utils;

/// <summary>Rewrites document-relative paths when a dependency project is saved to a different folder, so they keep
/// pointing at the same target instead of silently resolving against the new location.</summary>
internal static class RelativePathRebaser
{
    /// <summary>
    /// Rewrites a single path field. Empty values and already-absolute paths are returned unchanged — only
    /// relative paths are affected by moving the document. For
    /// <see cref="SaveAsRelativePathAction.ConvertToAbsolute"/> the relative path is resolved to its
    /// current absolute form; for <see cref="SaveAsRelativePathAction.RebaseRelative"/> it is
    /// re-expressed relative to <paramref name="newDirectory"/> (falling back to an absolute path across drives).
    /// </summary>
    /// <param name="value">The stored path value.</param>
    /// <param name="oldDirectory">The directory the value is currently resolved against.</param>
    /// <param name="newDirectory">The directory the value will be resolved against after the move.</param>
    /// <param name="action">The rewrite action to apply.</param>
    public static string Rewrite(string value, string oldDirectory, string newDirectory, SaveAsRelativePathAction action)
    {
        if (value.IsNullOrEmpty() || Path.IsPathFullyQualified(value))
        {
            return value;
        }

        var absolutePath = PathUtils.ResolveAsAbsolutePath(value, oldDirectory);

        if (action == SaveAsRelativePathAction.ConvertToAbsolute)
        {
            return absolutePath;
        }

        // RebaseRelative — re-express against the new directory. Path.GetRelativePath returns the absolute
        // path when the two paths are on different roots, so this degrades gracefully across drives.
        return newDirectory.IsNullOrEmpty()
            ? absolutePath
            : PathUtils.MakeRelativeIfPossible(absolutePath, newDirectory);
    }

    /// <summary>Returns the document-relative path fields that would be affected by saving to a new folder.</summary>
    /// <param name="document">The document to inspect.</param>
    public static IReadOnlyList<RelativePathField> GetRelativePathFields(DependencyProjectDocument document)
    {
        var fields = new List<RelativePathField>();

        AddIfRelative(fields, "Solution path", document.DiagramGenerator.Solution.SolutionPath);
        AddIfRelative(fields, "Export root", document.DiagramGenerator.Export.RootPath);
        AddIfRelative(fields, "Pre-generation working directory", document.PreGeneration.WorkingDirectory);
        AddIfRelative(fields, "Post-generation working directory", document.PostGeneration.WorkingDirectory);

        return fields;
    }

    /// <summary>Adds <paramref name="value"/> to <paramref name="fields"/> when it is a non-empty relative path.</summary>
    public static void AddIfRelative(ICollection<RelativePathField> fields, string label, string value)
    {
        if (IsRelative(value))
        {
            fields.Add(new RelativePathField(label, value));
        }
    }

    /// <summary>Whether <paramref name="value"/> is a non-empty relative path.</summary>
    public static bool IsRelative(string value) => value.IsNotNullOrEmpty() && !Path.IsPathFullyQualified(value);
}
