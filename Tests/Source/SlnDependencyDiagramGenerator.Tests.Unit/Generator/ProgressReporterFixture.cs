using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Generator;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator;

public class ProgressReporterFixture
{
    private readonly ProgressReporter _reporter = new();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void Should_Push_Message_To_OnProgress()
    {
        var messages = new List<string>();

        using var sub = _reporter.OnProgress.Subscribe(messages.Add);

        _reporter.Report("Hello, World!", _logger);

        messages.ShouldHaveSingleItem();
        messages[0].ShouldBe("Hello, World!");
    }

    [Fact]
    public void Should_Log_Message_As_Information()
    {
        _reporter.Report("Test message", _logger);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Should_Push_Multiple_Messages()
    {
        var messages = new List<string>();

        using var sub = _reporter.OnProgress.Subscribe(messages.Add);

        _reporter.Report("First", _logger);
        _reporter.Report("Second", _logger);
        _reporter.Report("Third", _logger);

        messages.Count.ShouldBe(3);
        messages.ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public void Should_Not_Push_When_Not_Subscribed()
    {
        // No subscription — should not throw.
        Should.NotThrow(() => _reporter.Report("No listeners", _logger));

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
