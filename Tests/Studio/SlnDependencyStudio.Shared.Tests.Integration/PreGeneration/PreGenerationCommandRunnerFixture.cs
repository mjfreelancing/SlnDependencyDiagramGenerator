using Shouldly;
using SlnDependencyStudio.Shared.Enumerations;
using SlnDependencyStudio.Shared.Tests.Integration.Support;

namespace SlnDependencyStudio.Shared.Tests.Integration.PreGeneration;

public class PreGenerationCommandRunnerFixture
{
    public class DisabledPreGeneration : PreGenerationCommandRunnerFixture
    {
        [Fact]
        public async Task Should_Not_Attempt_Command_When_Disabled()
        {
            var runner = IntegrationTestHarness.CreateRunner();
            var config = IntegrationTestHarness.CreateConfig(false, "cmd.exe", "/c echo hello");

            var result = await runner.RunAsync(config, CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.ExitCode.ShouldBe(0);
        }
    }

    public class SuccessfulPreGeneration : PreGenerationCommandRunnerFixture
    {
        [Fact]
        public async Task Should_Succeed_When_Command_Exits_Zero()
        {
            var runner = IntegrationTestHarness.CreateRunner();
            var config = IntegrationTestHarness.CreateConfig(true, "cmd.exe", "/c echo pre-generation command succeeded");

            var result = await runner.RunAsync(config, CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.ExitCode.ShouldBe(0);
        }
    }

    public class FailedPreGeneration : PreGenerationCommandRunnerFixture
    {
        [Fact]
        public async Task Should_Fail_When_Command_Exits_NonZero()
        {
            var runner = IntegrationTestHarness.CreateRunner();
            var config = IntegrationTestHarness.CreateConfig(true, "cmd.exe", "/c exit 42");

            var result = await runner.RunAsync(config, CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(42);
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }
    }

    public class CancelledPreGeneration : PreGenerationCommandRunnerFixture
    {
        [Fact]
        public async Task Should_Fail_When_Token_Is_PreCancelled()
        {
            var runner = IntegrationTestHarness.CreateRunner();
            var config = IntegrationTestHarness.CreateConfig(true, "cmd.exe", "/c echo should not run");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await runner.RunAsync(config, cts.Token);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(StudioExitCode.PreGenerationCommandCancelled.Value);
        }
    }

    public class WorkingDirectoryPreGeneration : PreGenerationCommandRunnerFixture
    {
        [Fact]
        public async Task Should_Execute_Command_In_Specified_WorkingDirectory()
        {
            using var tempDir = new DisposableTempDirectory("pregen-working-directory");
            var subDirName = "created-by-pregen";

            var runner = IntegrationTestHarness.CreateRunner();
            var config = IntegrationTestHarness.CreateConfig(
                true,
                "cmd.exe",
                $"/c mkdir {subDirName}",
                workingDirectory: tempDir.DirectoryPath);

            var result = await runner.RunAsync(config, CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            Directory.Exists(Path.Combine(tempDir.DirectoryPath, subDirName)).ShouldBeTrue();
        }
    }
}
