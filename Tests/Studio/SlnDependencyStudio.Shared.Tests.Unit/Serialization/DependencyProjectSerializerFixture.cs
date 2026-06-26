using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;
using System;

namespace SlnDependencyStudio.Shared.Tests.Unit.Serialization;

public class DependencyProjectSerializerFixture
{
    public class Serialize : DependencyProjectSerializerFixture
    {
        [Fact]
        public void Should_Produce_Valid_Json_With_SchemaVersion()
        {
            var serializer = CreateSerializer();
            var document = new DependencyProjectDocument
            {
                Metadata = new DependencyProjectMetadata
                {
                    ProjectName = "Test Project",
                    Description = "A test"
                }
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("\"schemaVersion\": 1");
            json.ShouldContain("Test Project");
            json.ShouldContain("A test");
        }

        [Fact]
        public void Should_Include_Generator_Config()
        {
            var serializer = CreateSerializer();
            var document = new DependencyProjectDocument
            {
                DiagramGenerator = new DependencyGeneratorConfig
                {
                    Projects = new GeneratorProjectOptions
                    {
                        SolutionPath = "test.sln",
                        RegexToInclude = [".*\\.csproj"]
                    }
                }
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("diagramGenerator");
            json.ShouldContain("test.sln");
            json.ShouldContain("regexToInclude");
        }

        [Fact]
        public void Should_Include_PreGeneration_Config()
        {
            var serializer = CreateSerializer();
            var document = new DependencyProjectDocument
            {
                PreGeneration = new PreGenerationConfig
                {
                    Enabled = true,
                    Command = "build.cmd",
                    Arguments = "--config Release",
                    WorkingDirectory = "./build",
                    ContinueOnFailure = false
                }
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("preGeneration");
            json.ShouldContain("build.cmd");
            json.ShouldContain("--config Release");
            json.ShouldContain("continueOnFailure");
        }
    }

    public class Deserialize : DependencyProjectSerializerFixture
    {
        [Fact]
        public void Should_Roundtrip_And_Match_Original()
        {
            var serializer = CreateSerializer();
            var original = new DependencyProjectDocument
            {
                SchemaVersion = 1,
                Metadata = new DependencyProjectMetadata
                {
                    ProjectName = "Roundtrip Test",
                    Description = "Testing roundtrip"
                },
                DiagramGenerator = new DependencyGeneratorConfig
                {
                    Projects = new GeneratorProjectOptions
                    {
                        SolutionPath = "solution.sln",
                        RegexToInclude = [".*\\.csproj"]
                    }
                },
                PreGeneration = new PreGenerationConfig
                {
                    Enabled = true,
                    Command = "pre.bat"
                }
            };

            var json = serializer.Serialize(original);
            var deserialized = serializer.Deserialize(json);

            deserialized.SchemaVersion.ShouldBe(original.SchemaVersion);
            deserialized.Metadata.ProjectName.ShouldBe(original.Metadata.ProjectName);
            deserialized.Metadata.Description.ShouldBe(original.Metadata.Description);
            deserialized.DiagramGenerator.Projects.SolutionPath.ShouldBe(original.DiagramGenerator.Projects.SolutionPath);
            deserialized.PreGeneration.Enabled.ShouldBe(original.PreGeneration.Enabled);
            deserialized.PreGeneration.Command.ShouldBe(original.PreGeneration.Command);
        }

        [Fact]
        public void Should_Throw_When_Schema_Version_Is_Too_New()
        {
            var serializer = CreateSerializer();
            var json = """
                {
                    "schemaVersion": 999,
                    "metadata": { "projectName": "Future", "description": "" },
                    "diagramGenerator": {},
                    "preGeneration": {}
                }
                """;

            var exception = Should.Throw<InvalidOperationException>(() => serializer.Deserialize(json));

            exception.Message.ShouldContain("999");
            exception.Message.ShouldContain("1");
        }

        [Fact]
        public void Should_Preserve_Unknown_Fields_On_Roundtrip()
        {
            var serializer = CreateSerializer();
            var json = """
                {
                    "schemaVersion": 1,
                    "metadata": { "projectName": "Ext", "description": "" },
                    "diagramGenerator": {},
                    "preGeneration": {},
                    "futureField": "will be preserved"
                }
                """;

            var deserialized = serializer.Deserialize(json);
            var reSerialized = serializer.Serialize(deserialized);

            reSerialized.ShouldContain("futureField");
            reSerialized.ShouldContain("will be preserved");
        }
    }

    private static DependencyProjectSerializer CreateSerializer()
    {
        return new DependencyProjectSerializer(new StudioJsonSerializer());
    }
}
