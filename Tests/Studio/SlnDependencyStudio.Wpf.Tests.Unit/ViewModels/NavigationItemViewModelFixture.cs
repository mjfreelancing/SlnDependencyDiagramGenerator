using AllOverIt.ReactiveUI.Factories;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
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
        public void Should_Have_HasUnsavedChanges_False()
        {
            _item.HasUnsavedChanges.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_Empty_StatusToolTip()
        {
            _item.StatusToolTip.ShouldBeEmpty();
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

    public class StatusToolTip : NavigationItemViewModelFixture
    {
        [Fact]
        public void Should_Describe_Unsaved_Changes_Only()
        {
            _item.HasUnsavedChanges = true;

            _item.StatusToolTip.ShouldBe("This section has unsaved changes");
        }

        [Fact]
        public void Should_Describe_Validation_Errors_Only()
        {
            _item.HasValidationError = true;

            _item.StatusToolTip.ShouldBe("This section has validation errors");
        }

        [Fact]
        public void Should_Describe_Both_When_Dirty_And_Error()
        {
            _item.HasUnsavedChanges = true;
            _item.HasValidationError = true;

            _item.StatusToolTip.ShouldBe("This section has validation errors and unsaved changes");
        }

        [Fact]
        public void Should_Describe_Both_Regardless_Of_Setting_Order()
        {
            _item.HasValidationError = true;
            _item.HasUnsavedChanges = true;

            _item.StatusToolTip.ShouldBe("This section has validation errors and unsaved changes");
        }

        [Fact]
        public void Should_Downgrade_To_Unsaved_Changes_When_Validation_Error_Cleared()
        {
            _item.HasUnsavedChanges = true;
            _item.HasValidationError = true;

            _item.HasValidationError = false;

            _item.StatusToolTip.ShouldBe("This section has unsaved changes");
        }

        [Fact]
        public void Should_Downgrade_To_Validation_Errors_When_Unsaved_Changes_Cleared()
        {
            _item.HasUnsavedChanges = true;
            _item.HasValidationError = true;

            _item.HasUnsavedChanges = false;

            _item.StatusToolTip.ShouldBe("This section has validation errors");
        }

        [Fact]
        public void Should_Clear_When_Status_Resolved()
        {
            _item.HasUnsavedChanges = true;
            _item.HasUnsavedChanges = false;

            _item.StatusToolTip.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Clear_When_All_Status_Resolved()
        {
            _item.HasUnsavedChanges = true;
            _item.HasValidationError = true;

            _item.HasUnsavedChanges = false;
            _item.HasValidationError = false;

            _item.StatusToolTip.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Raise_StatusToolTip_Changed_For_Unsaved_Changes()
        {
            var raised = false;
            _item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NavigationItemViewModel.StatusToolTip))
                {
                    raised = true;
                }
            };

            _item.HasUnsavedChanges = true;

            raised.ShouldBeTrue();
        }

        [Fact]
        public void Should_Raise_StatusToolTip_Changed_For_Validation_Error()
        {
            var raised = false;
            _item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NavigationItemViewModel.StatusToolTip))
                {
                    raised = true;
                }
            };

            _item.HasValidationError = true;

            raised.ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Raise_StatusToolTip_Changed_When_State_Unchanged()
        {
            _item.HasUnsavedChanges = true;

            var raised = false;
            _item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NavigationItemViewModel.StatusToolTip))
                {
                    raised = true;
                }
            };

            _item.HasUnsavedChanges = true;

            raised.ShouldBeFalse();
        }
    }
}
