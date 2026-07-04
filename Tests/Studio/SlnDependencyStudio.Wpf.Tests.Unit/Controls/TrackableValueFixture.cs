using Shouldly;
using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Controls;

[Collection(nameof(ReactiveUIInitializer))]
public class TrackableValueFixture
{
    public class SetOriginalValue : TrackableValueFixture
    {
        [Fact]
        public void Should_Set_Value()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("initial");

            trackable.Value.ShouldBe("initial");
        }

        [Fact]
        public void Should_Mark_IsDirty_False()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("hello");

            trackable.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Reset_IsDirty_When_Dirty_Then_Called_Again()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("hello");
            trackable.Value = "world";

            trackable.IsDirty.ShouldBeTrue();

            trackable.SetOriginalValue("world");

            trackable.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Handle_Same_Value_As_Current()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("same");
            trackable.SetOriginalValue("same");

            trackable.IsDirty.ShouldBeFalse();
            trackable.Value.ShouldBe("same");
        }

        [Fact]
        public void Should_Restore_Clean_State_After_Dirty_Then_Restore()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("alpha");
            trackable.Value = "beta";

            trackable.IsDirty.ShouldBeTrue();

            trackable.SetOriginalValue("beta");

            trackable.IsDirty.ShouldBeFalse();
            trackable.Value.ShouldBe("beta");
        }
    }

    public class IsDirty : TrackableValueFixture
    {
        [Fact]
        public void Should_Be_True_When_Value_Diverges()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("hello");
            trackable.Value = "world";

            trackable.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Value_Matches_Original()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("hello");
            trackable.Value = "world";
            trackable.Value = "hello";

            trackable.IsDirty.ShouldBeFalse();
        }
    }

    public class Dispose : TrackableValueFixture
    {
        [Fact]
        public void Should_Not_Throw()
        {
            var trackable = new TrackableValue<string>();

            trackable.SetOriginalValue("test");

            Should.NotThrow(trackable.Dispose);
        }
    }
}
