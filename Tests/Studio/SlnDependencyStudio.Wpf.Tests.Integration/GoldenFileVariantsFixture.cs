using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Tests.Integration.Support;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration;

/// <summary>Tests each golden file variant: deserialize and verify structure preserved on re-serialize.</summary>
public class GoldenFileVariantsFixture
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Theory]
    [InlineData("SampleProject.sds", "Sample Project")]
    [InlineData("SampleProject.D2.sds", "D2 Only")]
    [InlineData("SampleProject.Both.sds", "Both Formats")]
    [InlineData("SampleProject.WithPreGen.sds", "With Pre-Gen")]
    [InlineData("SampleProject.WithExclusions.sds", "With Exclusions")]
    public async Task Should_Deserialize_Golden_File(string fileName, string expectedProjectName)
    {
        var goldenFile = Path.Combine(FixturesDir, fileName);

        File.Exists(goldenFile).ShouldBeTrue($"Golden file not found: {goldenFile}");

        var provider = IntegrationTestHarness.CreateServiceProvider();
        var serializer = provider.GetRequiredService<IDependencyProjectSerializer>();

        var document = await serializer.DeserializeAsync(goldenFile);

        document.ShouldNotBeNull();
        document.SchemaVersion.ShouldBe(1);
        document.Metadata.ProjectName.ShouldBe(expectedProjectName);
        document.DiagramGenerator.Solution.SolutionPath.ShouldNotBeNullOrEmpty();
        document.DiagramGenerator.Diagram.Formats.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("SampleProject.sds")]
    [InlineData("SampleProject.D2.sds")]
    [InlineData("SampleProject.Both.sds")]
    [InlineData("SampleProject.WithPreGen.sds")]
    [InlineData("SampleProject.WithExclusions.sds")]
    public async Task Should_Preserve_Structure_On_ReSerialize(string fileName)
    {
        var goldenFile = Path.Combine(FixturesDir, fileName);

        var provider = IntegrationTestHarness.CreateServiceProvider();
        var serializer = provider.GetRequiredService<IDependencyProjectSerializer>();

        var original = await serializer.DeserializeAsync(goldenFile);

        using var tempFile = new DisposableTempFile(".sds");

        await serializer.SerializeAsync(original, tempFile.FilePath);

        var reloaded = await serializer.DeserializeAsync(tempFile.FilePath);

        reloaded.ShouldNotBeNull();
        reloaded.SchemaVersion.ShouldBe(original.SchemaVersion);
        reloaded.Metadata.ProjectName.ShouldBe(original.Metadata.ProjectName);
        reloaded.Metadata.Description.ShouldBe(original.Metadata.Description);
        reloaded.DiagramGenerator.Solution.SolutionPath.ShouldBe(original.DiagramGenerator.Solution.SolutionPath);
        reloaded.DiagramGenerator.Diagram.Formats.ShouldBe(original.DiagramGenerator.Diagram.Formats, ignoreOrder: true);
        reloaded.DiagramGenerator.Diagram.Direction.ShouldBe(original.DiagramGenerator.Diagram.Direction);
        reloaded.PreGeneration.Enabled.ShouldBe(original.PreGeneration.Enabled);
    }
}
