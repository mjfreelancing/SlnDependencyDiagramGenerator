using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared;
using SlnDependencyStudio.Shared.Serialization;
using Shouldly;
using System;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class DependencyProjectSerializerFixture
{
    public class Serialize : DependencyProjectSerializerFixture
    {
        [Fact]
        public void Should_Produce_Valid_Json_With_SchemaVersion()
        {
            var serializer = new DependencyProjectSerializer();
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
            var serializer = new DependencyProjectSerializer();
            var document = new DependencyProjectDocument
            {
                GeneratorConfig = new DependencyGeneratorConfig
                {
                    Projects = new GeneratorProjectOptions
                    {
                        SolutionPath = "test.sln",
                        RegexToInclude = [".*\\.csproj"]
                    }
                }
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("generatorConfig");
            json.ShouldContain("test.sln");
            json.ShouldContain("RegexToInclude");
        }

        [Fact]
        public void Should_Include_PreGeneration_Config()
        {
            var serializer = new DependencyProjectSerializer();
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
            json.ShouldContain("ContinueOnFailure");
        }
    }

    public class Deserialize : DependencyProjectSerializerFixture
    {
        [Fact]
        public void Should_Roundtrip_And_Match_Original()
        {
            var serializer = new DependencyProjectSerializer();
            var original = new DependencyProjectDocument
            {
                SchemaVersion = 1,
                Metadata = new DependencyProjectMetadata
                {
                    ProjectName = "Roundtrip Test",
                    Description = "Testing roundtrip"
                },
                GeneratorConfig = new DependencyGeneratorConfig
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
            deserialized.GeneratorConfig.Projects.SolutionPath.ShouldBe(original.GeneratorConfig.Projects.SolutionPath);
            deserialized.PreGeneration.Enabled.ShouldBe(original.PreGeneration.Enabled);
            deserialized.PreGeneration.Command.ShouldBe(original.PreGeneration.Command);
        }

        [Fact]
        public void Should_Throw_When_Schema_Version_Is_Too_New()
        {
            var serializer = new DependencyProjectSerializer();
            var json = """
                {
                    "schemaVersion": 999,
                    "metadata": { "projectName": "Future", "description": "" },
                    "generatorConfig": {},
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
            var serializer = new DependencyProjectSerializer();
            var json = """
                {
                    "schemaVersion": 1,
                    "metadata": { "projectName": "Ext", "description": "" },
                    "generatorConfig": {},
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
}