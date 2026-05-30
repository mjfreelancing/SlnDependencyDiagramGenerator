using SlnDependencyDiagramGenerator.Generator;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Unit;

public class TargetFrameworkBadgeProviderFixture
{
    public class GetBadge : TargetFrameworkBadgeProviderFixture
    {
        [Fact]
        public void Should_Return_A_Net8_Badge_Using_The_Blue_Palette_Color()
        {
            var provider = new TargetFrameworkBadgeProvider();

            var badge = provider.GetBadge("net8.0");

            badge.ShouldBe("![](https://img.shields.io/badge/.NET-8.0-55A9EE.svg)");
        }

        [Fact]
        public void Should_Return_A_Net9_Badge_Using_The_Green_Palette_Color_After_Net8()
        {
            var provider = new TargetFrameworkBadgeProvider();

            _ = provider.GetBadge("net8.0");

            var badge = provider.GetBadge("net9.0");

            badge.ShouldBe("![](https://img.shields.io/badge/.NET-9.0-6EBE50.svg)");
        }

        [Fact]
        public void Should_Return_A_Net10_Badge_Using_The_Purple_Palette_Color_After_Net8_And_Net9()
        {
            var provider = new TargetFrameworkBadgeProvider();

            _ = provider.GetBadge("net8.0");
            _ = provider.GetBadge("net9.0");

            var badge = provider.GetBadge("net10.0");

            badge.ShouldBe("![](https://img.shields.io/badge/.NET-10.0-C56EE0.svg)");
        }

        [Fact]
        public void Should_Strip_The_Platform_Suffix_When_Building_The_Badge_Message()
        {
            var provider = new TargetFrameworkBadgeProvider();

            var badge = provider.GetBadge("net10.0-windows10.0.19041");

            badge.ShouldBe("![](https://img.shields.io/badge/.NET-10.0--windows-55A9EE.svg)");
        }

        [Fact]
        public void Should_Return_The_Same_Badge_String_When_The_Same_Framework_Is_Requested_Twice()
        {
            var provider = new TargetFrameworkBadgeProvider();

            var first = provider.GetBadge("net10.0");
            var second = provider.GetBadge("net10.0");

            first.ShouldBe(second);
        }

        [Fact]
        public void Should_Assign_The_Same_Color_To_Frameworks_With_The_Same_Base_Moniker()
        {
            var provider = new TargetFrameworkBadgeProvider();

            var net10Badge = provider.GetBadge("net10.0");
            var net10WindowsBadge = provider.GetBadge("net10.0-windows10.0.19041");

            net10Badge.ShouldContain("-55A9EE.svg");
            net10WindowsBadge.ShouldContain("-55A9EE.svg");
        }
    }
}