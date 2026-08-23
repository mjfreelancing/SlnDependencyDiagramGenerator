using System.IO;

namespace SlnDependencyStudio.Wpf.Abstractions.IO;

/// <summary>Default implementation of <see cref="IFileSystem"/> that delegates directly
/// to <see cref="File"/> and <see cref="Directory"/>.</summary>
internal sealed class SystemFileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string text, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, text, cancellationToken);

    /// <inheritdoc />
    public void WriteAllText(string path, string text) => File.WriteAllText(path, text);

    /// <inheritdoc />
    public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <inheritdoc />
    public Stream OpenWrite(string path) => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public void MoveFile(string source, string destination, bool overwrite) => File.Move(source, destination, overwrite);
}
