using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyStudio.Shared.Tests.Unit.ProcessExecution;

public class PostGenerationCommandRunnerFixture
{
    [Fact]
    public async Task Should_Throw_When_Config_Is_Null()
    {
        var runner = new PostGenerationCommandRunner(Substitute.For<ILogger<PostGenerationCommandRunner>>());

        await Should.ThrowAsync<ArgumentNullException>(() => runner.RunAsync(null!, CancellationToken.None));
    }
}
