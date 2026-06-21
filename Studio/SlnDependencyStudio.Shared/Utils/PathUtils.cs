namespace SlnDependencyStudio.Shared.Utils;

/// <summary>Utility methods for resolving and validating file system paths.</summary>
public static class PathUtils
{
    /// <summary>Resolves a path to its fully-qualified absolute form. If <paramref name="path"/> is relative,
    /// it is combined with <paramref name="relativeToDirectory"/> and then resolved.</summary>
    /// <param name="path">The path to resolve. Can be absolute or relative.</param>
    /// <param name="relativeToDirectory">The directory to use as the base when <paramref name="path"/> is relative.</param>
    /// <returns>The fully-qualified absolute path.</returns>
    public static string ResolveAsAbsolutePath(string path, string relativeToDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(relativeToDirectory, path));
    }
}
