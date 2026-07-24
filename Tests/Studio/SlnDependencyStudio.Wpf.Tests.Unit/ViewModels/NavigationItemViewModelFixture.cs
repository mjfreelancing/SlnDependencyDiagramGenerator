using AllOverIt.ReactiveUI.Factories;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Wpf.ViewModels;

namespace SlnDependencyStudio.Wpf.Tests.Unit.ViewModels;

[Collection(nameof(ReactiveUIInitializer))]
public class NavigationItemViewModelFixture
{
    private sealed class TestNavigationItem : NavigationItemViewModel
    {
        public override Type ViewModelType => typeof(object);

        public override IViewFor CreateView(IViewFactory viewFactory) => null!;
    }

    private readonly TestNavigationItem _item = new();

    public class Construction : NavigationItemViewModelFixture
    {
        [Fact]
        public void Should_Have_Empty_DisplayName()
        {
            _item.DisplayName.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Have_HasValidationError_False()
        {
            _item.HasValidationError.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_IsAdvanced_False()
        {
            _item.IsAdvanced.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_ViewModelType()
        {
            _item.ViewModelType.ShouldBe(typeof(object));
        }
    }

    public class Properties : NavigationItemViewModelFixture
    {
        [Fact]
        public void Should_Raise_DisplayName_Changed()
        {
            var raised = false;
            _item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NavigationItemViewModel.DisplayName))
                {
                    raised = true;
                }
            };

            _item.DisplayName = "Test";

            raised.ShouldBeTrue();
        }
    }
}
