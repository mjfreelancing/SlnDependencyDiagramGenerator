using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Cli.Handlers;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Cli.Tests.Unit.Handlers;

public class CommandLineHandlerBaseFixture
{
    /// <summary>Test subclass that exposes protected methods for testing.</summary>
    private sealed class TestHandler : CommandLineHandlerBase
    {
        public TestHandler(IDependencyProjectSerializer serializer, ILogger logger)
            : base(serializer, logger)
        {
        }

        public new Task<DependencyProjectDocument> LoadDependencyProjectDocumentAsync(string projectFilename, CancellationToken cancellationToken)
            => base.LoadDependencyProjectDocumentAsync(projectFilename, cancellationToken);

        public new void WriteValidationErrors(ValidationException exception)
            => base.WriteValidationErrors(exception);

        public static string CallGetProjectDirectory(string path) => GetProjectDirectory(path);

        public override Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    public class GetProjectDirectory : CommandLineHandlerBaseFixture
    {
        [Fact]
        public void Should_Get_Project_Directory_From_File_Path()
        {
            var path = Path.Combine("some", "directory", "project.sds");

            var directory = TestHandler.CallGetProjectDirectory(path);

            directory.ShouldEndWith(Path.Combine("some", "directory"));
        }

        [Fact]
        public void Should_Throw_DirectoryNotFoundException_For_Root_Path()
        {
            // A root path like "C:\" has no directory component, which causes
            // Path.GetDirectoryName to return null.
            Should.Throw<DirectoryNotFoundException>(() => TestHandler.CallGetProjectDirectory(@"C:\"));
        }

        [Fact]
        public void Should_Return_Absolute_Path_When_Given_One()
        {
            var absolutePath = Path.Combine(Path.GetTempPath(), "project.sds");

            var directory = TestHandler.CallGetProjectDirectory(absolutePath);

            directory.ShouldBe(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    public class LoadDependencyProjectDocument : CommandLineHandlerBaseFixture
    {
        [Fact]
        public async Task Should_Resolve_WorkingDirectory_To_Absolute()
        {
            using var tempDir = new DisposableTempDirectory();
            var handler = CreateHandler(tempDir.DirectoryPath);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(tempDir.DirectoryPath, "project.sds"), CancellationToken.None);

            document.PreGeneration.WorkingDirectory.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "pregen")));
        }

        [Fact]
        public async Task Should_Resolve_PostGeneration_WorkingDirectory_To_Absolute()
        {
            using var tempDir = new DisposableTempDirectory();
            var handler = CreateHandler(tempDir.DirectoryPath);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(tempDir.DirectoryPath, "project.sds"), CancellationToken.None);

            document.PostGeneration.WorkingDirectory.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "postgen")));
        }

        [Fact]
        public async Task Should_Resolve_SolutionPath_To_Absolute()
        {
            using var tempDir = new DisposableTempDirectory();
            var handler = CreateHandler(tempDir.DirectoryPath);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(tempDir.DirectoryPath, "project.sds"), CancellationToken.None);

            document.DiagramGenerator.Solution.SolutionPath.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "MyApp.sln")));
        }

        [Fact]
        public async Task Should_Resolve_ExportRootPath_To_Absolute()
        {
            using var tempDir = new DisposableTempDirectory();
            var handler = CreateHandler(tempDir.DirectoryPath);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(tempDir.DirectoryPath, "project.sds"), CancellationToken.None);

            document.DiagramGenerator.Export.RootPath.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "output")));
        }

        [Fact]
        public async Task Should_Resolve_All_Paths_To_Absolute()
        {
            using var tempDir = new DisposableTempDirectory();
            var handler = CreateHandler(tempDir.DirectoryPath);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(tempDir.DirectoryPath, "project.sds"), CancellationToken.None);

            document.PreGeneration.WorkingDirectory.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "pregen")));

            document.PostGeneration.WorkingDirectory.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "postgen")));

            document.DiagramGenerator.Solution.SolutionPath.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "MyApp.sln")));

            document.DiagramGenerator.Export.RootPath.ShouldBe(Path.GetFullPath(Path.Combine(tempDir.DirectoryPath, "output")));
        }

        [Fact]
        public async Task Should_Keep_Absolute_Path_Unchanged()
        {
            var absoluteSlnPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "fixed.sln"));
            var serializer = Substitute.For<IDependencyProjectSerializer>();

            serializer
                .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(CreateDocument(
                    workingDir: @"C:\Tools\pregen",
                    solutionPath: absoluteSlnPath,
                    exportRoot: @"D:\Output",
                    postGenWorkingDir: @"E:\Deploy"));

            var logger = Substitute.For<ILogger>();
            var handler = new TestHandler(serializer, logger);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(Path.GetTempPath(), "project.sds"), CancellationToken.None);

            document.PreGeneration.WorkingDirectory.ShouldBe(@"C:\Tools\pregen");
            document.DiagramGenerator.Solution.SolutionPath.ShouldBe(absoluteSlnPath);
            document.DiagramGenerator.Export.RootPath.ShouldBe(@"D:\Output");
            document.PostGeneration.WorkingDirectory.ShouldBe(@"E:\Deploy");
        }

        [Fact]
        public async Task Should_Resolve_Empty_WorkingDirectory_To_Empty()
        {
            var serializer = Substitute.For<IDependencyProjectSerializer>();

            serializer
                .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(CreateDocument(
                    workingDir: string.Empty,
                    solutionPath: "MyApp.sln",
                    exportRoot: "output"));

            var logger = Substitute.For<ILogger>();
            var handler = new TestHandler(serializer, logger);

            var document = await handler.LoadDependencyProjectDocumentAsync(Path.Combine(Path.GetTempPath(), "project.sds"), CancellationToken.None);

            document.PreGeneration.WorkingDirectory.ShouldBe(string.Empty);
        }

        private static TestHandler CreateHandler(string tempDir, string? postGenWorkingDir = null)
        {
            var serializer = Substitute.For<IDependencyProjectSerializer>();

            serializer
                .DeserializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(CreateDocument(
                    workingDir: "pregen",
                    solutionPath: "MyApp.sln",
                    exportRoot: "output",
                    postGenWorkingDir: postGenWorkingDir ?? "postgen"));

            var logger = Substitute.For<ILogger>();

            return new TestHandler(serializer, logger);
        }
    }

    public class WriteValidationErrors : CommandLineHandlerBaseFixture
    {
        [Fact]
        public void Should_Log_Header_And_Each_Error()
        {
            var logger = Substitute.For<ILogger>();
            var serializer = Substitute.For<IDependencyProjectSerializer>();
            var handler = new TestHandler(serializer, logger);

            var errors = new List<ValidationFailure>
            {
                new("Command", "Command must not be empty."),
                new("Arguments", "Arguments contain invalid characters."),
                new("WorkingDirectory", "Working directory does not exist.")
            };

            var exception = new ValidationException(errors);

            handler.WriteValidationErrors(exception);

            // Verify header is logged
            logger.Received(1).Log(
                Arg.Is<LogLevel>(level => level == LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(obj => obj.ToString()!.Contains("Configuration validation failed")),
                null,
                Arg.Any<Func<object, Exception?, string>>());

            // Verify each error message is logged
            foreach (var error in errors)
            {
                logger.Received(1).Log(
                    Arg.Is<LogLevel>(level => level == LogLevel.Error),
                    Arg.Any<EventId>(),
                    Arg.Is<object>(obj => obj.ToString()!.Contains(error.ErrorMessage)),
                    null,
                    Arg.Any<Func<object, Exception?, string>>());
            }
        }

        [Fact]
        public void Should_Log_Nothing_When_No_Errors()
        {
            var logger = Substitute.For<ILogger>();
            var serializer = Substitute.For<IDependencyProjectSerializer>();
            var handler = new TestHandler(serializer, logger);

            var exception = new ValidationException([]);

            handler.WriteValidationErrors(exception);

            // Only the header should be logged
            logger.Received(1).Log(
                Arg.Is<LogLevel>(level => level == LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(obj => obj.ToString()!.Contains("Configuration validation failed")),
                null,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public void Should_Log_Single_Error()
        {
            var logger = Substitute.For<ILogger>();
            var serializer = Substitute.For<IDependencyProjectSerializer>();
            var handler = new TestHandler(serializer, logger);

            var errors = new List<ValidationFailure>
            {
                new("Command", "Command must not be empty.")
            };

            var exception = new ValidationException(errors);

            handler.WriteValidationErrors(exception);

            logger.Received(1).Log(
                Arg.Is<LogLevel>(level => level == LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(obj => obj.ToString()!.Contains("Configuration validation failed")),
                null,
                Arg.Any<Func<object, Exception?, string>>());

            logger.Received(1).Log(
                Arg.Is<LogLevel>(level => level == LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(obj => obj.ToString()!.Contains("Command must not be empty")),
                null,
                Arg.Any<Func<object, Exception?, string>>());
        }
    }

    private static DependencyProjectDocument CreateDocument(
        string workingDir, string solutionPath, string exportRoot, string? postGenWorkingDir = null)
    {
        return new DependencyProjectDocument
        {
            SchemaVersion = 1,
            Metadata = new DependencyProjectMetadata
            {
                ProjectName = "Test",
                Description = ""
            },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = [".*\\.csproj"]
                },
                Diagram = new GeneratorDiagramOptions
                {
                    Formats = [DiagramFormat.D2],
                    GroupName = "Test",
                    GroupNameAlias = "test",
                    Direction = GeneratorDiagramOptions.DiagramDirection.LR,
                    FrameworkStyle = new GeneratorDiagramOptions.FillStyle(),
                    PackageStyle = new GeneratorDiagramOptions.FillStyle(),
                    TransitiveStyle = new GeneratorDiagramOptions.FillStyle(),
                    Grouping = new GeneratorDiagramOptions.GroupingOptions
                    {
                        BackgroundStyle = new GeneratorDiagramOptions.FillStyle()
                    }
                },
                Export = new GeneratorExportOptions
                {
                    RootPath = exportRoot,
                    ImageFormats = [DiagramImageFormat.Png]
                }
            },
            PreGeneration = new PreGenerationConfig
            {
                Enabled = true,
                Command = "dotnet",
                WorkingDirectory = workingDir
            },
            PostGeneration = new PostGenerationConfig
            {
                Enabled = true,
                Command = "deploy.cmd",
                WorkingDirectory = postGenWorkingDir ?? string.Empty
            }
        };
    }
}
