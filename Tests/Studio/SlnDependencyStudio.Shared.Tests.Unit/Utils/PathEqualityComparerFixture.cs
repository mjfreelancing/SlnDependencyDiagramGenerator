using Shouldly;
using SlnDependencyStudio.Shared.Utils;

namespace SlnDependencyStudio.Shared.Tests.Unit.Utils;

public class PathEqualityComparerFixture
{
    private readonly PathEqualityComparer _comparer = PathEqualityComparer.Default;

    public class Equality : PathEqualityComparerFixture
    {
        [Fact]
        public void Should_Treat_Leading_Dot_Slash_As_Equivalent_To_Relative_Path()
        {
            _comparer.Equals(@".\Output", @"Output").ShouldBeTrue();
            _comparer.Equals(@"Output", @".\Output").ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Equate_Different_Paths()
        {
            _comparer.Equals(@".\Output", @".\Input").ShouldBeFalse();
        }

        [Fact]
        public void Should_Unify_Separators()
        {
            _comparer.Equals(@"Output/Diagrams", @"Output\Diagrams").ShouldBeTrue();
        }

        [Fact]
        public void Should_Ignore_Case_On_Windows_Style_Paths()
        {
            _comparer.Equals(@"C:\Docs\Output", @"c:\docs\output").ShouldBeTrue();
        }

        [Fact]
        public void Should_Ignore_Trailing_Separator()
        {
            _comparer.Equals(@"Output\", @"Output").ShouldBeTrue();
        }

        [Fact]
        public void Should_Preserve_Drive_Root()
        {
            _comparer.Equals(@"C:\", @"C:\").ShouldBeTrue();
        }
    }

    public class Hashing : PathEqualityComparerFixture
    {
        [Fact]
        public void Should_Return_Consistent_Hash_For_Equivalent_Paths()
        {
            _comparer.GetHashCode(@".\Output").ShouldBe(_comparer.GetHashCode(@"Output"));
        }
    }
}
