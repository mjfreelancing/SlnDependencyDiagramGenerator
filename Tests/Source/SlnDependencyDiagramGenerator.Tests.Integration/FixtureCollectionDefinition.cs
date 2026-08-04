namespace SlnDependencyDiagramGenerator.Tests.Integration;

/// <summary>
/// Groups all fixture-based integration tests into a single collection so the fixture solutions are
/// restored exactly once before any of them run, and so they run sequentially rather than in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class FixtureCollectionDefinition : ICollectionFixture<FixtureCollectionFixture>
{
    /// <summary>The collection name used by <c>[Collection(...)]</c> on each fixture-based test class.</summary>
    public const string Name = "Fixture solutions";
}
