using NSubstitute;
using Serilog.Events;
using Shouldly;
using SlnDependencyStudio.Shared.Logging;
using System;

namespace SlnDependencyStudio.Shared.Tests.Unit.Logging;

public class StudioLogSinkFixture
{
    [Fact]
    public void Should_Throw_When_Buffer_Null()
    {
        Should.Throw<ArgumentNullException>(() => new StudioLogSink(null!));
    }

    [Fact]
    public void Should_Forward_Rendered_Event_To_Buffer()
    {
        var buffer = Substitute.For<IStudioLogBuffer>();
        var sink = new StudioLogSink(buffer);

        sink.Emit(CreateLogEvent(LogEventLevel.Information, "Hello world"));

        buffer.Received(1).Add(Arg.Is<StudioLogEntry>(entry =>
            entry.Level == LogEventLevel.Information &&
            entry.Message == "Hello world" &&
            entry.ExceptionText == null));
    }

    [Fact]
    public void Should_Forward_Exception_Text_When_Present()
    {
        var buffer = Substitute.For<IStudioLogBuffer>();
        var sink = new StudioLogSink(buffer);

        var exception = new InvalidOperationException("boom");

        sink.Emit(CreateLogEvent(LogEventLevel.Error, "Failure", exception));

        buffer.Received(1).Add(Arg.Is<StudioLogEntry>(entry =>
            entry.Level == LogEventLevel.Error &&
            entry.ExceptionText!.Contains("boom")));
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, string message, Exception? exception = null)
    {
        var template = new Serilog.Parsing.MessageTemplateParser().Parse(message);

        return new LogEvent(DateTimeOffset.Now, level, exception, template, []);
    }
}
