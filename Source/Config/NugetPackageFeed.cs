namespace SlnDependencyDiagramGenerator.Config
{
    public sealed class NugetPackageFeed
    {
        // NuGet feed Uri
        public string SourceUri { get; init; }

        // Auth username. Null if not required.
        public string Username { get; init; }

        // Auth password. Null if not required.
        public string Password { get; init; }
    }
}