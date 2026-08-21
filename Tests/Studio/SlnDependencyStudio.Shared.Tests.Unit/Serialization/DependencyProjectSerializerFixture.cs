using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Exceptions;
using SlnDependencyStudio.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
                    Solution = new GeneratorSolutionOptions
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

        [Fact]
        public void Should_Include_RestoreSolution()
        {
            var serializer = CreateSerializer();
            var document = new DependencyProjectDocument
            {
                RestoreSolution = true
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("restoreSolution");
            json.ShouldContain("true");
        }

        [Fact]
        public void Should_Include_PostGeneration_Config()
        {
            var serializer = CreateSerializer();
            var document = new DependencyProjectDocument
            {
                PostGeneration = new PostGenerationConfig
                {
                    Enabled = true,
                    Command = "deploy.cmd",
                    Arguments = "--prod",
                    WorkingDirectory = "./dist"
                }
            };

            var json = serializer.Serialize(document);

            json.ShouldContain("postGeneration");
            json.ShouldContain("deploy.cmd");
            json.ShouldContain("--prod");
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
                    Solution = new GeneratorSolutionOptions
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
            deserialized.DiagramGenerator.Solution.SolutionPath.ShouldBe(original.DiagramGenerator.Solution.SolutionPath);
            deserialized.PreGeneration.Enabled.ShouldBe(original.PreGeneration.Enabled);
            deserialized.PreGeneration.Command.ShouldBe(original.PreGeneration.Command);
        }

        [Fact]
        public void Should_Roundtrip_RestoreSolution_And_PostGeneration()
        {
            var serializer = CreateSerializer();
            var original = new DependencyProjectDocument
            {
                RestoreSolution = true,
                PostGeneration = new PostGenerationConfig
                {
                    Enabled = true,
                    Command = "post.bat",
                    Arguments = "--cleanup",
                    WorkingDirectory = "./out"
                }
            };

            var json = serializer.Serialize(original);
            var deserialized = serializer.Deserialize(json);

            deserialized.RestoreSolution.ShouldBe(original.RestoreSolution);
            deserialized.PostGeneration.ShouldNotBeNull();
            deserialized.PostGeneration.Enabled.ShouldBe(original.PostGeneration.Enabled);
            deserialized.PostGeneration.Command.ShouldBe(original.PostGeneration.Command);
            deserialized.PostGeneration.Arguments.ShouldBe(original.PostGeneration.Arguments);
            deserialized.PostGeneration.WorkingDirectory.ShouldBe(original.PostGeneration.WorkingDirectory);
        }

        [Fact]
        public void Should_Default_RestoreSolution_To_True_And_PostGeneration_To_Disabled()
        {
            var serializer = CreateSerializer();
            var json = """
                {
                    "schemaVersion": 1,
                    "metadata": { "projectName": "Defaults", "description": "" },
                    "diagramGenerator": {}
                }
                """;

            var deserialized = serializer.Deserialize(json);

            deserialized.RestoreSolution.ShouldBeTrue();
            deserialized.PostGeneration.ShouldNotBeNull();
            deserialized.PostGeneration.Enabled.ShouldBeFalse();
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
            exception.Message.ShouldContain("Update the application");
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

        [Fact]
        public void Should_Throw_DependencyProjectException_When_Json_Is_Null_Literal()
        {
            var serializer = CreateSerializer();

            // System.Text.Json returns null for the JSON literal "null", which previously became a raw NRE.
            var exception = Should.Throw<DependencyProjectException>(() => serializer.Deserialize("null"));

            exception.Message.ShouldContain("empty or not a valid dependency project file");
        }

        [Fact]
        public void Should_Throw_JsonException_When_Json_Is_Empty()
        {
            var serializer = CreateSerializer();

            // Empty input is a parse failure (System.Text.Json), not a null result — the CLI already
            // maps JsonException to CannotLoadProjectFile, so empty files surface a clear error too.
            Should.Throw<JsonException>(() => serializer.Deserialize(string.Empty));
        }

        [Fact]
        public void Should_Throw_When_Migration_Path_Is_Missing()
        {
            var serializer = CreateSerializer();
            var json = """
                {
                    "schemaVersion": 0,
                    "metadata": { "projectName": "Legacy", "description": "" },
                    "diagramGenerator": {}
                }
                """;

            // A document below the current schema with no defined migration step must fail fast with a
            // clear message instead of silently loading at a stale version.
            var exception = Should.Throw<InvalidOperationException>(() => serializer.Deserialize(json));

            exception.Message.ShouldContain("schema version 0");
        }
    }

    public class Cancellation : DependencyProjectSerializerFixture
    {
        [Fact]
        public async Task Should_Propagate_Without_Logging_Error_When_SerializeAsync_Is_Cancelled()
        {
            var logger = Substitute.For<ILogger<DependencyProjectSerializer>>();
            var serializer = new DependencyProjectSerializer(new StudioJsonSerializer(), logger);

            using var tempFile = new DisposableTempFile(".sds", string.Empty);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // A fired token must propagate (as OperationCanceledException) without being logged as an error.
            await Should.ThrowAsync<OperationCanceledException>(() =>
                serializer.SerializeAsync(new DependencyProjectDocument(), tempFile.FilePath, cancellationTokenSource.Token));

            ShouldNotLogError(logger);
        }

        [Fact]
        public async Task Should_Propagate_Without_Logging_Error_When_DeserializeAsync_Is_Cancelled()
        {
            var logger = Substitute.For<ILogger<DependencyProjectSerializer>>();
            var serializer = new DependencyProjectSerializer(new StudioJsonSerializer(), logger);

            using var tempFile = new DisposableTempFile(".sds", "{}");
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // A fired token must propagate (as OperationCanceledException) without being logged as an error.
            await Should.ThrowAsync<OperationCanceledException>(() =>
                serializer.DeserializeAsync(tempFile.FilePath, cancellationTokenSource.Token));

            ShouldNotLogError(logger);
        }

        // Cancellation should propagate without being logged as an error: the first argument of every
        // ILogger.Log call is the LogLevel, so none of the recorded levels may be Error.
        private static void ShouldNotLogError(ILogger<DependencyProjectSerializer> logger)
        {
            var loggedLevels = logger.ReceivedCalls()
                .Select(call => call.GetArguments())
                .SelectMany(arguments => arguments.OfType<LogLevel>());

            loggedLevels.ShouldNotContain(LogLevel.Error);
        }
    }

    public class Migration : DependencyProjectSerializerFixture
    {
        [Fact]
        public void Should_Migrate_Document_To_Current_When_Path_Exists()
        {
            var migrations = new Dictionary<int, DependencyProjectSerializer.MigrationStep>
            {
                // A fake 0 -> 1 migration proving the chain walk applies the step and advances the version.
                { 0, new DependencyProjectSerializer.MigrationStep(ToVersion: 1, Apply: document => document.SchemaVersion = 1) }
            };

            var document = new DependencyProjectDocument { SchemaVersion = 0 };

            DependencyProjectSerializer.MigrateToCurrent(document, migrations);

            document.SchemaVersion.ShouldBe(1);
        }

        [Fact]
        public void Should_Throw_When_Migration_Does_Not_Advance_Declared_Version()
        {
            var migrations = new Dictionary<int, DependencyProjectSerializer.MigrationStep>
            {
                // The step declares it migrates to version 1 but its transform never bumps the version.
                { 0, new DependencyProjectSerializer.MigrationStep(ToVersion: 1, Apply: _ => { }) }
            };

            var document = new DependencyProjectDocument { SchemaVersion = 0 };

            var exception = Should.Throw<InvalidOperationException>(() =>
                DependencyProjectSerializer.MigrateToCurrent(document, migrations));

            exception.Message.ShouldContain("did not advance the document to the declared version 1");
        }
    }

    private static DependencyProjectSerializer CreateSerializer()
    {
        return new DependencyProjectSerializer(new StudioJsonSerializer(), Substitute.For<ILogger<DependencyProjectSerializer>>());
    }
}


