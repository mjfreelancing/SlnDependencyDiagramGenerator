using System;
using System.IO;

namespace SlnDependencyDiagramGenerator.Tests.Shared;

/// <summary>Creates a temporary directory that is deleted (recursively) when disposed.
/// This file is shared across test projects via a linked Compile Include.</summary>
internal sealed class DisposableTempDirectory : IDisposable
{
    /// <summary>The fully-qualified path of the created temporary directory.</summary>
    public string DirectoryPath { get; }

    /// <summary>Initializes a new instance, creating a unique temporary directory.</summary>
    /// <param name="name">An optional category name used to group related directories; omitted for a bare unique directory.</param>
    public DisposableTempDirectory(string? name = null)
    {
        DirectoryPath = string.IsNullOrEmpty(name)
            ? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            : Path.Combine(Path.GetTempPath(), name, Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(DirectoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
