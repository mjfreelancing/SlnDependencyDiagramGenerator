using Shouldly;
using SlnDependencyStudio.Wpf.Models;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Models;

public class StudioThemeFixture
{
    public class SingletonInstances : StudioThemeFixture
    {
        [Fact]
        public void Light_Should_Have_Value_1()
        {
            StudioTheme.Light.Value.ShouldBe(1);
        }

        [Fact]
        public void Dark_Should_Have_Value_2()
        {
            StudioTheme.Dark.Value.ShouldBe(2);
        }

        [Fact]
        public void Light_Should_Have_Name_Light()
        {
            StudioTheme.Light.Name.ShouldBe("Light");
        }

        [Fact]
        public void Dark_Should_Have_Name_Dark()
        {
            StudioTheme.Dark.Name.ShouldBe("Dark");
        }

        [Fact]
        public void Light_And_Dark_Should_Not_Be_Same()
        {
            StudioTheme.Light.ShouldNotBeSameAs(StudioTheme.Dark);
        }
    }

    public class From : StudioThemeFixture
    {
        [Fact]
        public void Should_Return_Light_When_Name_Is_Light()
        {
            var result = StudioTheme.From("Light");
            result.ShouldBeSameAs(StudioTheme.Light);
        }

        [Fact]
        public void Should_Return_Dark_When_Name_Is_Dark()
        {
            var result = StudioTheme.From("Dark");
            result.ShouldBeSameAs(StudioTheme.Dark);
        }
    }
}
