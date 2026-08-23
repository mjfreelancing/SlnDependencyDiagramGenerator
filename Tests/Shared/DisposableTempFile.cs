using System;
using System.IO;

namespace SlnDependencyDiagramGenerator.Tests.Shared;

/// <summary>Creates a temporary file (optionally with content) that is deleted when disposed.
/// This file is shared across test projects via a linked Compile Include.</summary>
internal sealed class DisposableTempFile : IDisposable
{
    /// <summary>The fully-qualified path of the created temporary file.</summary>
    public string FilePath { get; }

    /// <summary>Initializes a new instance, creating a unique temporary file with the specified extension.</summary>
    /// <param name="extension">The file extension, including the leading dot (for example <c>".sln"</c>).</param>
    /// <param name="content">Optional content written to the file; when <see langword="null"/> an empty file is created.</param>
    public DisposableTempFile(string extension, string? content = null)
    {
        FilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

        if (content is null)
        {
            File.WriteAllText(FilePath, string.Empty);
        }
        else
        {
            File.WriteAllText(FilePath, content);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
