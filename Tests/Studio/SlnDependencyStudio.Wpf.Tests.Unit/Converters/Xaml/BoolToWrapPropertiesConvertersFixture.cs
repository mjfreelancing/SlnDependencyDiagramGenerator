using Shouldly;
using SlnDependencyStudio.Wpf.Converters.Xaml;
using System.Globalization;
using System.Windows;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Converters.Xaml;

public class BoolToTextWrappingConverterFixture
{
    private readonly BoolToTextWrappingConverter _converter = new();

    [Fact]
    public void Should_Return_Wrap_When_True()
    {
        var result = _converter.Convert(true, typeof(TextWrapping), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(TextWrapping.Wrap);
    }

    [Fact]
    public void Should_Return_NoWrap_When_False()
    {
        var result = _converter.Convert(false, typeof(TextWrapping), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(TextWrapping.NoWrap);
    }

    [Fact]
    public void Should_Return_NoWrap_When_Null()
    {
        var result = _converter.Convert(null!, typeof(TextWrapping), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(TextWrapping.NoWrap);
    }

    [Fact]
    public void ConvertBack_Should_Throw()
    {
        Should.Throw<NotSupportedException>(() =>
            _converter.ConvertBack(null!, typeof(bool), null!, CultureInfo.InvariantCulture));
    }
}
