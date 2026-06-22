namespace SlnDependencyStudio.Shared.Tests.Integration.Support;

/// <summary>Creates a temporary directory that is deleted when disposed.</summary>
internal sealed class DisposableTempDirectory : IDisposable
{
    public string DirectoryPath { get; }

    public DisposableTempDirectory(string name)
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "SlnDependencyStudio.Shared.Tests.Integration",
            name,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(DirectoryPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
