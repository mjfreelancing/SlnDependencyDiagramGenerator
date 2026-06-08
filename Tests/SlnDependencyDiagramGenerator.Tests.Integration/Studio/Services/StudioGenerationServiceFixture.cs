using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared;
using SlnDependencyStudio.Shared.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Studio.Services;

public class StudioGenerationServiceFixture
{
    public class RunAsync : StudioGenerationServiceFixture
    {
        [Fact]
        public async Task Should_Not_Run_PreGeneration_When_Disabled()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            var preGen = new PreGenerationConfig
            {
                Enabled = false,
                Command = "nonexistent.exe"
            };

            var result = await service.RunAsync(config, preGen, CancellationToken.None);

            result.PreGenerationRan.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Not_Run_PreGeneration_When_Command_Empty()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            var preGen = new PreGenerationConfig
            {
                Enabled = true,
                Command = string.Empty
            };

            var result = await service.RunAsync(config, preGen, CancellationToken.None);

            result.PreGenerationRan.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Return_Failure_When_PreGeneration_Fails_Without_Continue()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            var preGen = new PreGenerationConfig
            {
                Enabled = true,
                Command = "nonexistent_command_xyz_12345",
                ContinueOnFailure = false
            };

            var result = await service.RunAsync(config, preGen, CancellationToken.None);

            result.Success.ShouldBeFalse();
            result.PreGenerationRan.ShouldBeTrue();
            result.PreGenerationSucceeded.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Skip_PreGeneration_When_Null_Config()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            var result = await service.RunAsync(config, null, CancellationToken.None);

            result.PreGenerationRan.ShouldBeFalse();
            result.PreGenerationSucceeded.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Continue_When_PreGeneration_Fails_With_ContinueOnFailure()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            var preGen = new PreGenerationConfig
            {
                Enabled = true,
                Command = "nonexistent_command_xyz_12345",
                ContinueOnFailure = true
            };

            var result = await service.RunAsync(config, preGen, CancellationToken.None);

            result.PreGenerationRan.ShouldBeTrue();
            result.PreGenerationSucceeded.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Throw_When_Cancellation_Requested()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(
                () => service.RunAsync(config, null, cts.Token));
        }

        [Fact]
        public async Task Should_Mark_PreGeneration_Succeeded_When_Command_Exits_Zero()
        {
            var generator = new DependencyGenerator();
            var service = new StudioGenerationService(generator);

            var config = new DependencyGeneratorConfig
            {
                Projects = new GeneratorProjectOptions { SolutionPath = "nonexistent.sln" }
            };

            // dotnet --version succeeds on any machine with the SDK installed.
            var preGen = new PreGenerationConfig
            {
                Enabled = true,
                Command = "dotnet",
                Arguments = "--version",
                ContinueOnFailure = false
            };

            var result = await service.RunAsync(config, preGen, CancellationToken.None);

            result.PreGenerationRan.ShouldBeTrue();
            result.PreGenerationSucceeded.ShouldBeTrue();
        }
    }
}
