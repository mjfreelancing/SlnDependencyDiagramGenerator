using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Tests.Integration.Support;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration;

/// <summary>End-to-end document serialization roundtrip tests:
/// create → serialize to file → deserialize → verify fields match.</summary>
public class DocumentRoundtripFixture
{
    [Fact]
    public async Task Should_Roundtrip_Basic_Document()
    {
        var tempFile = Path.GetTempFileName() + ".sds";

        try
        {
            var provider = IntegrationTestHarness.CreateServiceProvider();
            var serializer = provider.GetRequiredService<IDependencyProjectSerializer>();

            var original = IntegrationTestHarness.CreateDocument(
                projectName: "Roundtrip Test",
                solutionPath: @"C:\Projects\test.sln",
                format: DiagramFormat.D2);

            // Serialize to file
            await serializer.SerializeAsync(original, tempFile);

            // Deserialize from file
            var loaded = await serializer.DeserializeAsync(tempFile);

            // Verify
            loaded.ShouldNotBeNull();
            loaded.SchemaVersion.ShouldBe(1);
            loaded.Metadata.ProjectName.ShouldBe("Roundtrip Test");
            loaded.Metadata.Description.ShouldBe("Integration test project");
            loaded.DiagramGenerator.Solution.SolutionPath.ShouldBe(@"C:\Projects\test.sln");
            loaded.DiagramGenerator.Diagram.Formats.ShouldContain(DiagramFormat.D2);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Should_Preserve_ExtensionData_On_Roundtrip()
    {
        var tempFile = Path.GetTempFileName() + ".sds";

        try
        {
            var provider = IntegrationTestHarness.CreateServiceProvider();
            var serializer = provider.GetRequiredService<IDependencyProjectSerializer>();

            // Write a JSON file with an unknown field to test forward compatibility
            var json = """
            {
              "schemaVersion": 1,
              "metadata": {
                "projectName": "Forward Compat",
                "description": "Testing unknown fields"
              },
              "diagramGenerator": {
                "solution": { "solutionPath": "test.sln" },
                "diagram": { "formats": ["Mermaid"], "direction": "LR" },
                "export": { "rootPath": "%TEMP%\\out", "clearContents": true }
              },
              "preGeneration": { "enabled": false, "command": "", "arguments": "", "workingDirectory": "", "continueOnFailure": false },
              "futureField": "should be preserved",
              "futureObject": { "nested": "value" }
            }
            """;

            await File.WriteAllTextAsync(tempFile, json);

            // Deserialize
            var loaded = await serializer.DeserializeAsync(tempFile);

            loaded.ShouldNotBeNull();
            loaded.Metadata.ProjectName.ShouldBe("Forward Compat");

            // Re-serialize
            var reJson = serializer.Serialize(loaded);

            // Verify the unknown fields are preserved in the output
            reJson.ShouldContain("futureField");
            reJson.ShouldContain("futureObject");
            reJson.ShouldContain("nested");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Should_Deserialize_Golden_File()
    {
        var goldenDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var goldenFile = Path.Combine(goldenDir, "SampleProject.sds");

        File.Exists(goldenFile).ShouldBeTrue($"Golden file not found: {goldenFile}");

        var provider = IntegrationTestHarness.CreateServiceProvider();
        var serializer = provider.GetRequiredService<IDependencyProjectSerializer>();

        var document = await serializer.DeserializeAsync(goldenFile);

        document.ShouldNotBeNull();
        document.SchemaVersion.ShouldBe(1);
        document.Metadata.ProjectName.ShouldBe("Sample Project");
        document.Metadata.Description.ShouldBe("A sample project for integration testing");
        document.DiagramGenerator.Solution.SolutionPath.ShouldBe(@"C:\Projects\Sample\Sample.sln");
        document.DiagramGenerator.Diagram.Formats.ShouldContain(DiagramFormat.Mermaid);
        document.DiagramGenerator.Diagram.Direction.ShouldBe(GeneratorDiagramOptions.DiagramDirection.LR);
        document.PreGeneration.Enabled.ShouldBeFalse();
    }
}
