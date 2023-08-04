namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Contains configuration for a nuget package feed.</summary>
    public sealed class NugetPackageFeed
    {
        /// <summary>The NuGet feed Uri.</summary>
        public string SourceUri { get; init; }

        /// <summary>The authentication username. Set to null if not required.</summary>
        public string Username { get; init; }

        /// <summary>The authentication password. Set to null if not required.</summary>
        public string Password { get; init; }
    }
}