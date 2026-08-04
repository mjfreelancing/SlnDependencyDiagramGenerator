using SlnDependencyDiagramGenerator.Tests.Integration.Support;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

/// <summary>
/// Restores the fixture solutions once before any test in the collection runs so each fixture
/// project has its <c>obj/project.assets.json</c> file.
/// </summary>
public sealed class FixtureCollectionFixture : IAsyncLifetime
{
    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return FixtureRestorer.RestoreAllAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
