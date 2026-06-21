using Shouldly;
using SlnDependencyStudio.Cli.Handlers;

namespace SlnDependencyStudio.Cli.Tests.Unit.Handlers;

public class CommandLineHandlerBaseFixture
{
    /// <summary>Test subclass that exposes the protected static method for testing.</summary>
    private sealed class TestHandler : CommandLineHandlerBase
    {
        private TestHandler() : base(null!, null!) { }

        public static string CallGetConfigDirectory(string path) => GetConfigDirectory(path);

        public override Task HandleAsync(string configFilename, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    [Fact]
    public void Should_Get_Config_Directory_From_File_Path()
    {
        var path = Path.Combine("some", "directory", "config.sds");

        var directory = TestHandler.CallGetConfigDirectory(path);

        directory.ShouldEndWith(Path.Combine("some", "directory"));
    }

    [Fact]
    public void Should_Throw_DirectoryNotFoundException_For_Root_Path()
    {
        // A root path like "C:\" has no directory component, which causes
        // Path.GetDirectoryName to return null.
        Should.Throw<DirectoryNotFoundException>(() => TestHandler.CallGetConfigDirectory(@"C:\"));
    }

    [Fact]
    public void Should_Return_Absolute_Path_When_Given_One()
    {
        var absolutePath = Path.Combine(Path.GetTempPath(), "config.sds");

        var directory = TestHandler.CallGetConfigDirectory(absolutePath);

        directory.ShouldBe(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
    }
}
