using AllOverIt.Logging;
using NSubstitute;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Tests.Integration.Support;
using Shouldly;
using System.Threading;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

public class CancellationScenariosFixture
{
    public class GeneratorCancellation : CancellationScenariosFixture
    {
        [Fact]
        public async Task Should_Throw_OperationCanceledException_When_PreCancelled_Token_Is_Provided()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");
            options.Formats = [DiagramFormat.D2, DiagramFormat.Mermaid];

            using var tempDirectory = IntegrationTestHarness.CreateTempDirectory("cancellation-pre-cancelled");

            var solutionPath = IntegrationTestHarness.GetFixtureSolutionPath("Basic", ".slnx");
            var configuration = IntegrationTestHarness.CreateConfig(solutionPath, tempDirectory.DirectoryPath, options);
            var generator = new DependencyGenerator(configuration, Substitute.For<IColorConsoleLogger>());

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var exception = await Should.ThrowAsync<OperationCanceledException>(() =>
                generator.CreateDiagramsAsync(cancellationTokenSource.Token));

            exception.ShouldNotBeNull();
        }
    }
}
