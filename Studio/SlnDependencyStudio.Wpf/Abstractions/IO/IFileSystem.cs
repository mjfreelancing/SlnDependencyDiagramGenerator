namespace SlnDependencyStudio.Wpf.Abstractions.IO;

/// <summary>Abstraction over the file system.</summary>
public interface IFileSystem
{
    /// <summary>Returns <see langword="true"/> when a file exists at the specified path.</summary>
    bool FileExists(string path);

    /// <summary>Returns <see langword="true"/> when a directory exists at the specified path.</summary>
    bool DirectoryExists(string path);

    /// <summary>Writes text to the specified file, creating or overwriting it.</summary>
    Task WriteAllTextAsync(string path, string text, CancellationToken ct = default);
}
