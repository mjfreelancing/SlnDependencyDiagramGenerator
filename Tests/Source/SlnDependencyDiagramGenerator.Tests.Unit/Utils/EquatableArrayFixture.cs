using Shouldly;
using SlnDependencyDiagramGenerator.Utils;
using System;
using System.Linq;
using Xunit;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Utils;

public class EquatableArrayFixture
{
    public class Equality : EquatableArrayFixture
    {
        [Fact]
        public void Should_Be_Equal_When_Content_Matches()
        {
            var first = new EquatableArray<string>([".*", ".*Tests.*"]);
            var second = new EquatableArray<string>([".*", ".*Tests.*"]);

            first.Equals(second).ShouldBeTrue();
            first.Equals((object)second).ShouldBeTrue();
            first.GetHashCode().ShouldBe(second.GetHashCode());
        }

        [Fact]
        public void Should_Be_Equal_When_Both_Are_Empty()
        {
            var first = new EquatableArray<string>([]);
            var second = new EquatableArray<string>([]);

            first.Equals(second).ShouldBeTrue();
            first.GetHashCode().ShouldBe(second.GetHashCode());
        }

        [Fact]
        public void Should_Be_Equal_When_Both_Are_Null()
        {
            var first = new EquatableArray<string>(null);
            var second = new EquatableArray<string>(null);

            first.Equals(second).ShouldBeTrue();
            first.GetHashCode().ShouldBe(second.GetHashCode());
        }

        [Fact]
        public void Should_Not_Be_Equal_When_Content_Differs()
        {
            var first = new EquatableArray<string>([".*"]);
            var second = new EquatableArray<string>([".*Tests.*"]);

            first.Equals(second).ShouldBeFalse();
            first.Equals((object)second).ShouldBeFalse();
        }

        [Fact]
        public void Should_Not_Be_Equal_When_Length_Differs()
        {
            var first = new EquatableArray<string>([".*"]);
            var second = new EquatableArray<string>([".*", ".*Tests.*"]);

            first.Equals(second).ShouldBeFalse();
        }

        [Fact]
        public void Should_Not_Be_Equal_When_Compared_To_Null_Array()
        {
            var first = new EquatableArray<string>([".*"]);
            var second = new EquatableArray<string>(null);

            first.Equals(second).ShouldBeFalse();
        }
    }

    public class Access : EquatableArrayFixture
    {
        [Fact]
        public void Should_Expose_Count_And_Indexer()
        {
            var array = new EquatableArray<string>(["a", "b", "c"]);

            array.Count.ShouldBe(3);
            array[0].ShouldBe("a");
            array[2].ShouldBe("c");
        }

        [Fact]
        public void Should_Return_New_Array_From_ToArray()
        {
            var array = new EquatableArray<string>(["a", "b"]);

            var copy = array.ToArray();

            copy.ShouldBe(["a", "b"]);
            copy.Length.ShouldBe(2);
        }

        [Fact]
        public void Should_Enumerate_Elements()
        {
            var array = new EquatableArray<string>(["a", "b", "c"]);

            array.ToList().ShouldBe(["a", "b", "c"]);
        }
    }
}
