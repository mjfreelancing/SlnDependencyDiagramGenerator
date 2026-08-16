using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyStudio.Shared.Tests.Unit.ProcessExecution;

public class PreGenerationCommandRunnerFixture
{
    [Fact]
    public async Task Should_Throw_When_Config_Is_Null()
    {
        var runner = new PreGenerationCommandRunner(Substitute.For<ILogger<PreGenerationCommandRunner>>());

        await Should.ThrowAsync<ArgumentNullException>(() => runner.RunAsync(null!, CancellationToken.None));
    }
}
