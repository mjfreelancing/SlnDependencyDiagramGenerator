using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Features.Project;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project;

public class DependencyProjectServiceFixture
{
    private readonly IDependencyProjectSerializer _serializer = Substitute.For<IDependencyProjectSerializer>();
    private readonly DependencyProjectService _service;

    public DependencyProjectServiceFixture()
    {
        _service = new DependencyProjectService(_serializer);
    }

    public class CreateFromDefaults : DependencyProjectServiceFixture
    {
        [Fact]
        public void Should_Return_New_Document()
        {
            var result = _service.CreateFromDefaults();

            result.ShouldNotBeNull();
            result.ShouldBeOfType<DependencyProjectDocument>();
        }

        [Fact]
        public void Should_Return_Document_With_Default_SchemaVersion()
        {
            var result = _service.CreateFromDefaults();

            result.SchemaVersion.ShouldBe(1);
        }
    }

    public class OpenAsync : DependencyProjectServiceFixture
    {
        [Fact]
        public async Task Should_Delegate_To_Serializer()
        {
            var filePath = @"C:\Projects\test.sds";
            var expected = new DependencyProjectDocument();

            _serializer.DeserializeAsync(filePath, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await _service.OpenAsync(filePath);

            result.ShouldBeSameAs(expected);

            await _serializer.Received(1).DeserializeAsync(filePath, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Pass_CancellationToken()
        {
            var filePath = @"C:\Projects\test.sds";
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            _serializer.DeserializeAsync(filePath, token).Returns(new DependencyProjectDocument());

            await _service.OpenAsync(filePath, token);

            await _serializer.Received(1).DeserializeAsync(filePath, token);
        }
    }

    public class SaveAsync : DependencyProjectServiceFixture
    {
        [Fact]
        public async Task Should_Delegate_To_Serializer()
        {
            var document = new DependencyProjectDocument();
            var filePath = @"C:\Projects\test.sds";

            await _service.SaveAsync(document, filePath);

            await _serializer.Received(1).SerializeAsync(document, filePath, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Pass_CancellationToken()
        {
            var document = new DependencyProjectDocument();
            var filePath = @"C:\Projects\test.sds";
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            await _service.SaveAsync(document, filePath, token);

            await _serializer.Received(1).SerializeAsync(document, filePath, token);
        }
    }
}
