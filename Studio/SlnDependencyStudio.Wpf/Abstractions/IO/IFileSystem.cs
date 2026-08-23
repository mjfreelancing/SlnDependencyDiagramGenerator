using System.IO;

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

    /// <summary>Writes text to the specified file synchronously, creating or overwriting it.</summary>
    void WriteAllText(string path, string text);

    /// <summary>Opens an existing file for reading.</summary>
    Stream OpenRead(string path);

    /// <summary>Creates or opens a file for writing.</summary>
    Stream OpenWrite(string path);

    /// <summary>Creates a directory and all missing parents.</summary>
    void CreateDirectory(string path);

    /// <summary>Moves a file, optionally overwriting the destination.</summary>
    void MoveFile(string source, string destination, bool overwrite);
}
