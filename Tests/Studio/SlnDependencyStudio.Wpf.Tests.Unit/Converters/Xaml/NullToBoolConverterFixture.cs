using Shouldly;
using SlnDependencyStudio.Wpf.Converters.Xaml;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Converters.Xaml;

public class NullToBoolConverterFixture
{
    private readonly NullToBoolConverter _converter = new();

    public class Convert : NullToBoolConverterFixture
    {
        [Fact]
        public void Should_Return_False_For_Null()
        {
            var result = (bool)_converter.Convert(null!, null!, null!, null!);
            result.ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_True_For_Non_Null_String()
        {
            var result = (bool)_converter.Convert("hello", null!, null!, null!);
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_True_For_Empty_String()
        {
            var result = (bool)_converter.Convert(string.Empty, null!, null!, null!);
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_True_For_Integer()
        {
            var result = (bool)_converter.Convert(42, null!, null!, null!);
            result.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_True_For_Object()
        {
            var result = (bool)_converter.Convert(new object(), null!, null!, null!);
            result.ShouldBeTrue();
        }
    }

    public class ConvertBack : NullToBoolConverterFixture
    {
        [Fact]
        public void Should_Throw_NotSupportedException()
        {
            Should.Throw<NotSupportedException>(() => _converter.ConvertBack(true, null!, null!, null!));
        }
    }
}
