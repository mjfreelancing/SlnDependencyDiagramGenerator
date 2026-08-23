using AllOverIt.Extensions;

namespace SlnDependencyStudio.Shared.Utils;

/// <summary>Utility methods for resolving and validating file system paths.</summary>
public static class PathUtils
{
    /// <summary>Resolves a path to its fully-qualified absolute form. If <paramref name="path"/> is relative,
    /// it is combined with <paramref name="relativeToDirectory"/> and then resolved.</summary>
    /// <param name="path">The path to resolve. Can be absolute or relative.</param>
    /// <param name="relativeToDirectory">The directory to use as the base when <paramref name="path"/> is relative.</param>
    /// <returns>The fully-qualified absolute path, or <c>string.Empty</c> when null/empty.</returns>
    public static string ResolveAsAbsolutePath(string? path, string relativeToDirectory)
    {
        if (path.IsNullOrEmpty())
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(relativeToDirectory, path));
    }

    /// <summary>Returns a relative path if <paramref name="path"/> can be expressed relative to
    /// <paramref name="relativeToDirectory"/>; otherwise returns <paramref name="path"/> unchanged.
    /// Uses <see cref="Path.GetRelativePath"/> which handles both child and ancestor paths
    /// (producing <c>..\</c> segments when the path is above the base directory) and returns
    /// the original path when the two paths are on different roots.</summary>
    /// <param name="path">The absolute path to potentially make relative.</param>
    /// <param name="relativeToDirectory">The base directory to compute the relative path against.</param>
    /// <returns>A relative path when possible; the original <paramref name="path"/> when inputs are empty
    /// or the path is on a different drive/UNC share.</returns>
    public static string MakeRelativeIfPossible(string path, string relativeToDirectory)
    {
        if (path.IsNullOrEmpty() || relativeToDirectory.IsNullOrEmpty())
        {
            return path;
        }

        return Path.GetRelativePath(relativeToDirectory, path);
    }
}
