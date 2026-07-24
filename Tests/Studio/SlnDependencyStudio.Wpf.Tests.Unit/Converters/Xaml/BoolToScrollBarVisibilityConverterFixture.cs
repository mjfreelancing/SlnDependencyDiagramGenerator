using Shouldly;
using SlnDependencyStudio.Wpf.Converters.Xaml;
using System.Globalization;
using System.Windows.Controls;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Converters.Xaml;

public class BoolToScrollBarVisibilityConverterFixture
{
    private readonly BoolToScrollBarVisibilityConverter _converter = new();

    [Fact]
    public void Should_Return_Disabled_When_True()
    {
        var result = _converter.Convert(true, typeof(ScrollBarVisibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(ScrollBarVisibility.Disabled);
    }

    [Fact]
    public void Should_Return_Auto_When_False()
    {
        var result = _converter.Convert(false, typeof(ScrollBarVisibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(ScrollBarVisibility.Auto);
    }

    [Fact]
    public void Should_Return_Auto_When_Null()
    {
        var result = _converter.Convert(null!, typeof(ScrollBarVisibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(ScrollBarVisibility.Auto);
    }

    [Fact]
    public void ConvertBack_Should_Throw()
    {
        Should.Throw<NotSupportedException>(() =>
            _converter.ConvertBack(null!, typeof(bool), null!, CultureInfo.InvariantCulture));
    }
}
