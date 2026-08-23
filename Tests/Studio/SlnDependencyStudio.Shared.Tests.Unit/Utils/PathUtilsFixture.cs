using Shouldly;
using SlnDependencyStudio.Shared.Utils;
using System.IO;

namespace SlnDependencyStudio.Shared.Tests.Unit.Utils;

public class PathUtilsFixture
{
    public class ResolveAsAbsolutePath : PathUtilsFixture
    {
        [Fact]
        public void Should_Return_Absolute_Path_When_Already_Absolute()
        {
            var absolutePath = Path.GetFullPath(@"C:\Projects\test.sln");

            var result = PathUtils.ResolveAsAbsolutePath(absolutePath, @"C:\Some\Other");

            result.ShouldBe(absolutePath);
        }

        [Fact]
        public void Should_Combine_Relative_Path_With_Base_Directory()
        {
            var result = PathUtils.ResolveAsAbsolutePath(@"SubDir\test.sln", @"C:\Projects");

            result.ShouldBe(Path.GetFullPath(@"C:\Projects\SubDir\test.sln"));
        }

        [Fact]
        public void Should_Handle_Dot_Relative_Path()
        {
            var result = PathUtils.ResolveAsAbsolutePath(@".\test.sln", @"C:\Projects");

            result.ShouldBe(Path.GetFullPath(@"C:\Projects\test.sln"));
        }

        [Fact]
        public void Should_Handle_Parent_Directory_Relative_Path()
        {
            var result = PathUtils.ResolveAsAbsolutePath(@"..\test.sln", @"C:\Projects\Sub");

            result.ShouldBe(Path.GetFullPath(@"C:\Projects\test.sln"));
        }
    }

    public class MakeRelativeIfPossible : PathUtilsFixture
    {
        [Fact]
        public void Should_Return_Relative_Path_For_Child()
        {
            var result = PathUtils.MakeRelativeIfPossible(
                @"C:\Projects\Sub\test.sln",
                @"C:\Projects");

            result.ShouldBe(@"Sub\test.sln");
        }

        [Fact]
        public void Should_Return_Parent_Relative_When_Ancestor()
        {
            var result = PathUtils.MakeRelativeIfPossible(
                @"C:\Projects\test.sln",
                @"C:\Projects\Sub");

            result.ShouldBe(@"..\test.sln");
        }

        [Fact]
        public void Should_Return_Original_When_Empty_Path()
        {
            var result = PathUtils.MakeRelativeIfPossible(string.Empty, @"C:\Projects");

            result.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Return_Original_When_Empty_Base()
        {
            var result = PathUtils.MakeRelativeIfPossible(@"C:\Projects\test.sln", string.Empty);

            result.ShouldBe(@"C:\Projects\test.sln");
        }

        [Fact]
        public void Should_Return_Original_When_Different_Roots()
        {
            var result = PathUtils.MakeRelativeIfPossible(@"D:\Other\test.sln", @"C:\Projects");

            result.ShouldBe(@"D:\Other\test.sln");
        }
    }
}
