using Shouldly;
using SlnDependencyDiagramGenerator.Tests.Shared;
using SlnDependencyStudio.Shared.Enumerations;
using SlnDependencyStudio.Shared.Tests.Integration.Support;

namespace SlnDependencyStudio.Shared.Tests.Integration.RestoreSolution;

public class RestoreSolutionRunnerFixture
{
    public class SuccessfulRestore : RestoreSolutionRunnerFixture
    {
        [Fact]
        public async Task Should_Succeed_When_Solution_Is_Valid()
        {
            using var tempDir = new DisposableTempDirectory("restore-success");

            var solutionPath = CreateRestoreableSolution(tempDir.DirectoryPath);

            var runner = IntegrationTestHarness.CreateRestoreRunner();

            var result = await runner.RunAsync(solutionPath, CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.ExitCode.ShouldBe(0);
        }
    }

    public class FailedRestore : RestoreSolutionRunnerFixture
    {
        [Fact]
        public async Task Should_Fail_When_Solution_Does_Not_Exist()
        {
            using var tempDir = new DisposableTempDirectory("restore-failure");

            var runner = IntegrationTestHarness.CreateRestoreRunner();

            var result = await runner.RunAsync(
                Path.Combine(tempDir.DirectoryPath, "missing.sln"),
                CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldNotBe(0);
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }
    }

    public class CancelledRestore : RestoreSolutionRunnerFixture
    {
        [Fact]
        public async Task Should_Report_Cancelled_When_Token_Is_PreCancelled()
        {
            var runner = IntegrationTestHarness.CreateRestoreRunner();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await runner.RunAsync(@"C:\whatever\solution.sln", cts.Token);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(StudioExitCode.PreGenerationCommandCancelled.Value);
        }
    }

    public class ExitCodeMapping : RestoreSolutionRunnerFixture
    {
        [Fact]
        public async Task Should_Surface_Actual_Exit_Code_On_Failure()
        {
            using var tempDir = new DisposableTempDirectory("restore-exitcode");

            var runner = IntegrationTestHarness.CreateRestoreRunner();

            var result = await runner.RunAsync(
                Path.Combine(tempDir.DirectoryPath, "missing.sln"),
                CancellationToken.None);

            result.Succeeded.ShouldBeFalse();

            // A normal dotnet restore failure surfaces the real (non-zero) process exit code.
            // The DotNetRestoreFailed code is reserved for unexpected errors (e.g. the process cannot start).
            result.ExitCode.ShouldNotBe(0);
            result.ExitCode.ShouldNotBe(StudioExitCode.DotNetRestoreFailed.Value);
        }
    }

    private static string CreateRestoreableSolution(string directoryPath)
    {
        var appDirectory = Path.Combine(directoryPath, "SampleApp");
        Directory.CreateDirectory(appDirectory);

        File.WriteAllText(Path.Combine(appDirectory, "SampleApp.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var solutionPath = Path.Combine(directoryPath, "SampleApp.slnx");

        File.WriteAllText(solutionPath, """
            <Solution>
              <Project Path="SampleApp/SampleApp.csproj" />
            </Solution>
            """);

        return solutionPath;
    }
}
