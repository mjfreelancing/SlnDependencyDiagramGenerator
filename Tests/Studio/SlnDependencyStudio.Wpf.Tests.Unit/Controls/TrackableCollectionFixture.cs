using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Controls;

[Collection(nameof(ReactiveUIInitializer))]
public class TrackableCollectionFixture : IDisposable
{
    private readonly TrackableCollection<string> _collection = new();

    public class Construction : TrackableCollectionFixture
    {
        [Fact]
        public void Should_Have_Empty_Items()
        {
            _collection.Items.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Have_IsDirty_False()
        {
            _collection.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalItems : TrackableCollectionFixture
    {
        [Fact]
        public void Should_Populate_Items()
        {
            _collection.SetOriginalItems(["a", "b"]);

            _collection.Items.ShouldBe(["a", "b"]);
        }

        [Fact]
        public void Should_Mark_IsDirty_False()
        {
            _collection.SetOriginalItems(["a", "b"]);

            _collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Clear_Previous_Items()
        {
            _collection.SetOriginalItems(["a", "b"]);
            _collection.SetOriginalItems(["c"]);

            _collection.Items.ShouldBe(["c"]);
        }

        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _collection.SetOriginalItems(["a"]);
            _collection.Items.Add("b");

            _collection.IsDirty.ShouldBeTrue();

            _collection.SetOriginalItems(["a", "b"]);

            _collection.IsDirty.ShouldBeFalse();
        }
    }

    public class IsDirty : TrackableCollectionFixture
    {
        [Fact]
        public void Should_Be_False_When_Empty_And_Baseline_Empty()
        {
            _collection.SetOriginalItems([]);

            _collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_Item_Added()
        {
            _collection.SetOriginalItems([]);
            _collection.Items.Add("a");

            _collection.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_Item_Removed()
        {
            _collection.SetOriginalItems(["a", "b"]);
            _collection.Items.Remove("a");

            _collection.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_Cleared()
        {
            _collection.SetOriginalItems(["a", "b"]);
            _collection.Items.Clear();

            _collection.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_To_Clean_When_Edit_Reverted()
        {
            _collection.SetOriginalItems(["a", "b"]);
            _collection.Items.Add("c");

            _collection.IsDirty.ShouldBeTrue();

            _collection.Items.Remove("c");

            _collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Ignore_Order_When_Comparing()
        {
            _collection.SetOriginalItems(["b", "a"]);

            _collection.Items.Clear();
            _collection.Items.Add("a");
            _collection.Items.Add("b");

            _collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_False_When_Cleared_Without_SetOriginalItems()
        {
            _collection.Items.Add("a");
            _collection.Items.Add("b");
            _collection.Items.Clear();

            _collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Detect_Different_Count()
        {
            _collection.SetOriginalItems(["a"]);
            _collection.Items.Add("b");

            _collection.IsDirty.ShouldBeTrue();
        }
    }

    public class Load : TrackableCollectionFixture
    {
        [Fact]
        public void Should_Replace_All_Items()
        {
            _collection.SetOriginalItems(["a", "b"]);
            _collection.Load(["x", "y", "z"]);

            _collection.Items.ShouldBe(["x", "y", "z"]);
        }

        [Fact]
        public void Should_Not_Reset_Baseline()
        {
            _collection.SetOriginalItems(["a"]);
            _collection.Load(["b"]);

            _collection.IsDirty.ShouldBeTrue();
        }
    }

    public class ToArray : TrackableCollectionFixture
    {
        [Fact]
        public void Should_Return_Current_Items()
        {
            _collection.SetOriginalItems(["a", "b"]);

            _collection.ToArray().ShouldBe(["a", "b"]);
        }

        [Fact]
        public void Should_Return_Empty_Array_When_No_Items()
        {
            _collection.ToArray().ShouldBeEmpty();
        }
    }

    public class CustomComparer : TrackableCollectionFixture
    {
        private static IComparer<SampleValue> CreateComparer() =>
            Comparer<SampleValue>.Create((first, second) => StringComparer.Ordinal.Compare(first.Name, second.Name));

        [Fact]
        public void Should_Ignore_Order_For_Non_Comparable_Values()
        {
            using var collection = new TrackableCollection<SampleValue>(CreateComparer());

            collection.SetOriginalItems([new SampleValue("b"), new SampleValue("a")]);
            collection.Items.Clear();
            collection.Items.Add(new SampleValue("a"));
            collection.Items.Add(new SampleValue("b"));

            collection.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Detect_Dirty_For_Non_Comparable_Values()
        {
            using var collection = new TrackableCollection<SampleValue>(CreateComparer());

            collection.SetOriginalItems([new SampleValue("a")]);
            collection.Items.Add(new SampleValue("b"));

            collection.IsDirty.ShouldBeTrue();
        }
    }

    /// <summary>A value type that is equatable (via record value equality) but not <see cref="IComparable{T}"/>,
    /// used to verify the collection's optional custom comparer supports non-comparable types.</summary>
    private sealed record SampleValue(string Name);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }
}
