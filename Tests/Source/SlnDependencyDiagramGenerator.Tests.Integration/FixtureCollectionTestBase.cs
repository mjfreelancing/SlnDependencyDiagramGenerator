namespace SlnDependencyDiagramGenerator.Tests.Integration;

/// <summary>
/// Base class for fixture-based integration test classes. Carries the
/// <see cref="CollectionAttribute"/> for the fixture-solution collection so every derived test
/// class joins the collection (and the fixture restore it triggers) without repeating the
/// attribute on each test class.
/// </summary>
/// <remarks>
/// The collection attribute is inherited through the type hierarchy, so a new test project only
/// needs to derive from this base to opt into the restore.
/// </remarks>
[Collection(FixtureCollectionDefinition.Name)]
public abstract class FixtureCollectionTestBase
{
}
