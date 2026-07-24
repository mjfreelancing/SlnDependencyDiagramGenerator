using Shouldly;
using SlnDependencyStudio.Wpf.Features.CardSession;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.CardSession;

public class CardSessionStateFixture
{
    private readonly CardSessionState _state = new();

    public class IsExpanded : CardSessionStateFixture
    {
        [Fact]
        public void Should_Return_True_For_Unset_Key()
        {
            _state.IsExpanded("unknown").ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_Set_Value()
        {
            _state.SetExpanded("card1", true);
            _state.IsExpanded("card1").ShouldBeTrue();

            _state.SetExpanded("card2", false);
            _state.IsExpanded("card2").ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_Updated_Value_When_Overwritten()
        {
            _state.SetExpanded("card1", false);
            _state.IsExpanded("card1").ShouldBeFalse();

            _state.SetExpanded("card1", true);
            _state.IsExpanded("card1").ShouldBeTrue();
        }
    }

    public class SetExpanded : CardSessionStateFixture
    {
        [Fact]
        public void Should_Not_Throw_When_Setting_Multiple_Keys()
        {
            Should.NotThrow(() =>
            {
                _state.SetExpanded("a", true);
                _state.SetExpanded("b", false);
                _state.SetExpanded("c", true);
            });
        }
    }
}
