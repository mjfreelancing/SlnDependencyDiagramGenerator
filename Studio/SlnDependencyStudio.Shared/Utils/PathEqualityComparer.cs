namespace SlnDependencyStudio.Shared.Utils;

/// <summary>
/// Compares file system paths by their normalized form, so equivalent spellings of the same
/// path — such as <c>.\Output</c> vs <c>Output</c>, <c>/</c> vs <c>\</c> separators, or differing
/// case on Windows — compare as equal.
/// </summary>
public sealed class PathEqualityComparer : IEqualityComparer<string>
{
    /// <summary>A shared instance.</summary>
    public static PathEqualityComparer Default { get; } = new();

    /// <inheritdoc />
    public bool Equals(string? x, string? y)
    {
        if (x is null || y is null)
        {
            return x == y;
        }

        return Normalize(x) == Normalize(y);
    }

    /// <inheritdoc />
    public int GetHashCode(string obj)
    {
        return Normalize(obj).GetHashCode();
    }

    private static string Normalize(string path)
    {
        // Unify directory separators.
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        // Drop a redundant leading ".\" segment (e.g. ".\Output" -> "Output").
        if (normalized.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        // Trim trailing separators, preserving a drive root such as "C:\".
        normalized = normalized.TrimEnd(Path.DirectorySeparatorChar);

        if (normalized.Length == 2 && normalized[1] == Path.VolumeSeparatorChar)
        {
            normalized += Path.DirectorySeparatorChar;
        }

        // Windows file systems are case-insensitive.
        return normalized.ToLowerInvariant();
    }
}
