using AllOverIt.Logging;
using NSubstitute;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Tests.Unit.Support;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class DependencyGeneratorCancellationFixture
{
    public class CreateDiagramsAsync : DependencyGeneratorCancellationFixture
    {
        [Fact]
        public async Task Should_Throw_OperationCanceledException_When_PreCancelled_Token_Is_Provided()
        {
            // Create a minimal .sln file so that config validation passes
            var tempSlnPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sln");
            try
            {
                await File.WriteAllTextAsync(tempSlnPath,
                    """
                    Microsoft Visual Studio Solution File, Format Version 12.00
                    """);

                var config = new TestConfigBuilder()
                    .WithSolutionPath(tempSlnPath)
                    .Build();

                var generator = new DependencyGenerator(config, Substitute.For<IColorConsoleLogger>());

                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Cancel();

                var exception = await Should.ThrowAsync<OperationCanceledException>(() =>
                    generator.CreateDiagramsAsync(cancellationTokenSource.Token));

                exception.ShouldNotBeNull();
            }
            finally
            {
                if (File.Exists(tempSlnPath))
                {
                    File.Delete(tempSlnPath);
                }
            }
        }
    }
}
