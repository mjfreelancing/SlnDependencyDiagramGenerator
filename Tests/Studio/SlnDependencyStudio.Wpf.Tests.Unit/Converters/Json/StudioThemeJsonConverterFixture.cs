using Shouldly;
using SlnDependencyStudio.Wpf.Converters.Json;
using SlnDependencyStudio.Wpf.Models;
using System.Text.Json;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Converters.Json;

public class StudioThemeJsonConverterFixture
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new StudioThemeJsonConverter() }
    };

    public class Serialize : StudioThemeJsonConverterFixture
    {
        [Fact]
        public void Should_Serialize_Light()
        {
            var json = JsonSerializer.Serialize(StudioTheme.Light, Options);
            json.ShouldBe("\"Light\"");
        }

        [Fact]
        public void Should_Serialize_Dark()
        {
            var json = JsonSerializer.Serialize(StudioTheme.Dark, Options);
            json.ShouldBe("\"Dark\"");
        }
    }

    public class Deserialize : StudioThemeJsonConverterFixture
    {
        [Fact]
        public void Should_Deserialize_Light()
        {
            var result = JsonSerializer.Deserialize<StudioTheme>("\"Light\"", Options);
            result.ShouldBeSameAs(StudioTheme.Light);
        }

        [Fact]
        public void Should_Deserialize_Dark()
        {
            var result = JsonSerializer.Deserialize<StudioTheme>("\"Dark\"", Options);
            result.ShouldBeSameAs(StudioTheme.Dark);
        }

        [Fact]
        public void Should_Return_Null_For_Null()
        {
            var result = JsonSerializer.Deserialize<StudioTheme>("null", Options);
            result.ShouldBeNull();
        }
    }

    public class Roundtrip : StudioThemeJsonConverterFixture
    {
        [Fact]
        public void Should_Roundtrip_Light()
        {
            var json = JsonSerializer.Serialize(StudioTheme.Light, Options);
            var result = JsonSerializer.Deserialize<StudioTheme>(json, Options);
            result.ShouldBeSameAs(StudioTheme.Light);
        }

        [Fact]
        public void Should_Roundtrip_Dark()
        {
            var json = JsonSerializer.Serialize(StudioTheme.Dark, Options);
            var result = JsonSerializer.Deserialize<StudioTheme>(json, Options);
            result.ShouldBeSameAs(StudioTheme.Dark);
        }
    }
}
