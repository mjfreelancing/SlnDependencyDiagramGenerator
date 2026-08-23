using Shouldly;
using SlnDependencyStudio.Shared.Serialization;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SlnDependencyStudio.Shared.Tests.Unit.Serialization;

public class StudioJsonSerializerFixture
{
    private sealed record TestRecord
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    private enum TestEnum
    {
        Default,
        Custom
    }

    private sealed record ModelWithEnum
    {
        public TestEnum Value { get; init; }
    }

    public class Serialize : StudioJsonSerializerFixture
    {
        [Fact]
        public void Should_Produce_CamelCase_Json()
        {
            var serializer = new StudioJsonSerializer();
            var value = new TestRecord { Id = 42, Name = "Test" };

            var json = serializer.Serialize(value);

            json.ShouldContain("\"id\": 42");
            json.ShouldContain("\"name\": \"Test\"");
        }

        [Fact]
        public void Should_Serialize_Null_Property()
        {
            var serializer = new StudioJsonSerializer();
            var value = new TestRecord { Id = 1, Name = "NonNull", Description = null };

            var json = serializer.Serialize(value);

            json.ShouldContain("\"description\": null");
        }

        [Fact]
        public void Should_Format_Enums_As_Strings()
        {
            var serializer = new StudioJsonSerializer();
            var value = new ModelWithEnum { Value = TestEnum.Custom };

            var json = serializer.Serialize(value);

            json.ShouldContain("\"value\": \"Custom\"");
        }

        [Fact]
        public void Should_Produce_Indented_Output()
        {
            var serializer = new StudioJsonSerializer();
            var value = new TestRecord { Id = 1, Name = "A" };

            var json = serializer.Serialize(value);

            json.ShouldContain(Environment.NewLine);
        }

        [Fact]
        public void Should_Serialize_Empty_Object()
        {
            var serializer = new StudioJsonSerializer();

            var json = serializer.Serialize(new object());

            json.ShouldBe("{}");
        }
    }

    public class SerializeAsync : StudioJsonSerializerFixture
    {
        [Fact]
        public async Task Should_Write_CamelCase_Json_To_Stream()
        {
            var serializer = new StudioJsonSerializer();
            var value = new TestRecord { Id = 99, Name = "AsyncTest" };

            await using var stream = new MemoryStream();
            await serializer.SerializeAsync(stream, value, TestContext.Current.CancellationToken);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

            json.ShouldContain("\"id\": 99");
            json.ShouldContain("\"name\": \"AsyncTest\"");
        }

        [Fact]
        public async Task Should_Throw_On_Null_Stream()
        {
            var serializer = new StudioJsonSerializer();

            await Should.ThrowAsync<ArgumentNullException>(() =>
                serializer.SerializeAsync<TestRecord>(null!, new TestRecord()));
        }
    }

    public class Deserialize : StudioJsonSerializerFixture
    {
        [Fact]
        public void Should_Deserialize_CamelCase_Json()
        {
            var serializer = new StudioJsonSerializer();
            var json = """{"id":42,"name":"Test","description":"hello"}""";

            var result = serializer.Deserialize<TestRecord>(json);

            result.ShouldNotBeNull();
            result!.Id.ShouldBe(42);
            result.Name.ShouldBe("Test");
            result.Description.ShouldBe("hello");
        }

        [Fact]
        public void Should_Return_Null_For_Null_Literal()
        {
            var serializer = new StudioJsonSerializer();

            var result = serializer.Deserialize<TestRecord>("null");

            result.ShouldBeNull();
        }

        [Fact]
        public void Should_Deserialize_Enum_From_String()
        {
            var serializer = new StudioJsonSerializer();
            var json = """{"value":"Custom"}""";

            var result = serializer.Deserialize<ModelWithEnum>(json);

            result.ShouldNotBeNull();
            result!.Value.ShouldBe(TestEnum.Custom);
        }
    }

    public class DeserializeAsync : StudioJsonSerializerFixture
    {
        [Fact]
        public async Task Should_Deserialize_From_Stream_Async()
        {
            var serializer = new StudioJsonSerializer();
            var json = """{"id":10,"name":"Async","description":"async stream"}""";
            var bytes = Encoding.UTF8.GetBytes(json);

            using var stream = new MemoryStream(bytes);
            var result = await serializer.DeserializeAsync<TestRecord>(stream, TestContext.Current.CancellationToken);

            result.ShouldNotBeNull();
            result!.Id.ShouldBe(10);
            result.Name.ShouldBe("Async");
            result.Description.ShouldBe("async stream");
        }
    }

    public class Roundtrip : StudioJsonSerializerFixture
    {
        [Fact]
        public void Should_Roundtrip_Object_Through_String()
        {
            var serializer = new StudioJsonSerializer();
            var original = new TestRecord { Id = 100, Name = "Roundtrip", Description = "full cycle" };

            var json = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<TestRecord>(json);

            deserialized.ShouldNotBeNull();
            deserialized!.Id.ShouldBe(original.Id);
            deserialized.Name.ShouldBe(original.Name);
            deserialized.Description.ShouldBe(original.Description);
        }

        [Fact]
        public async Task Should_Roundtrip_Object_Through_Stream()
        {
            var serializer = new StudioJsonSerializer();
            var original = new TestRecord { Id = 200, Name = "StreamRoundtrip", Description = "via stream" };

            await using var stream = new MemoryStream();
            await serializer.SerializeAsync(stream, original, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var deserialized = await serializer.DeserializeAsync<TestRecord>(stream, TestContext.Current.CancellationToken);

            deserialized.ShouldNotBeNull();
            deserialized!.Id.ShouldBe(original.Id);
            deserialized.Name.ShouldBe(original.Name);
            deserialized.Description.ShouldBe(original.Description);
        }
    }
}
