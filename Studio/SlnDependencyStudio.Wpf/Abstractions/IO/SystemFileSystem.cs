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
}
