using Shouldly;
using SlnDependencyStudio.Wpf.Converters.Xaml;
using System.Windows.Media;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Converters.Xaml;

public class HexToColorConverterFixture
{
    private readonly HexToColorConverter _converter = HexToColorConverter.Instance;

    public class Convert : HexToColorConverterFixture
    {
        [Fact]
        public void Should_Convert_Valid_Hex_With_Hash()
        {
            var result = (Color)_converter.Convert("#FF0000", null, null, null!);
            result.ShouldBe(Color.FromRgb(255, 0, 0));
        }

        [Fact]
        public void Should_Convert_Valid_Hex_Without_Hash()
        {
            var result = (Color)_converter.Convert("00FF00", null, null, null!);
            result.ShouldBe(Color.FromRgb(0, 255, 0));
        }

        [Fact]
        public void Should_Return_Transparent_For_Null()
        {
            var result = (Color)_converter.Convert(null, null, null, null!);
            result.ShouldBe(Colors.Transparent);
        }

        [Fact]
        public void Should_Return_Transparent_For_Empty_String()
        {
            var result = (Color)_converter.Convert(string.Empty, null, null, null!);
            result.ShouldBe(Colors.Transparent);
        }

        [Fact]
        public void Should_Return_Transparent_For_Whitespace()
        {
            var result = (Color)_converter.Convert("   ", null, null, null!);
            result.ShouldBe(Colors.Transparent);
        }

        [Fact]
        public void Should_Return_Transparent_For_Invalid_Hex()
        {
            var result = (Color)_converter.Convert("not-a-color", null, null, null!);
            result.ShouldBe(Colors.Transparent);
        }

        [Fact]
        public void Should_Return_Transparent_For_Non_String_Value()
        {
            var result = (Color)_converter.Convert(42, null, null, null!);
            result.ShouldBe(Colors.Transparent);
        }
    }

    public class ConvertBack : HexToColorConverterFixture
    {
        [Fact]
        public void Should_Throw_NotSupportedException()
        {
            Should.Throw<NotSupportedException>(() =>
                _converter.ConvertBack(Colors.Red, null, null, null!));
        }
    }
}
