using Shouldly;
using SlnDependencyStudio.Wpf.Components;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.Collections.ObjectModel;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Components;

[Collection(nameof(ReactiveUIInitializer))]
public class TagInputModelFixture
{
    private readonly ObservableCollection<string> _items = [];
    private readonly TagInputModel _model;

    public TagInputModelFixture()
    {
        _model = new TagInputModel(_items);
    }

    public class Construction : TagInputModelFixture
    {
        [Fact]
        public void Should_Have_Empty_NewItemText()
        {
            _model.NewItemText.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Have_Empty_Items()
        {
            _model.Items.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Have_Null_Error()
        {
            _model.Error.ShouldBeNull();
        }
    }

    public class AddCommand : TagInputModelFixture
    {
        [Fact]
        public void Should_Add_Item_When_Text_Is_Non_Empty()
        {
            _model.NewItemText = "test-item";

            _model.AddCommand.Execute().Subscribe();

            _items.ShouldContain("test-item");
            _model.NewItemText.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Not_Add_When_Text_Is_Whitespace()
        {
            _model.NewItemText = "   ";

            _model.AddCommand.Execute().Subscribe();

            _items.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Not_Add_When_Text_Is_Empty()
        {
            _model.AddCommand.Execute().Subscribe();

            _items.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Trim_Input_Before_Adding()
        {
            _model.NewItemText = "  trimmed  ";

            _model.AddCommand.Execute().Subscribe();

            _items.ShouldContain("trimmed");
            _items.ShouldNotContain("  trimmed  ");
        }
    }

    public class RemoveCommand : TagInputModelFixture
    {
        [Fact]
        public void Should_Remove_Existing_Item()
        {
            _items.Add("item1");
            _items.Add("item2");

            _model.RemoveCommand.Execute("item1").Subscribe();

            _items.ShouldNotContain("item1");
            _items.ShouldContain("item2");
        }
    }

    public class Validation : TagInputModelFixture
    {
        [Fact]
        public void Should_Set_Error_When_Validator_Fails()
        {
            var model = new TagInputModel([], text => text == "bad" ? "Invalid input" : null)
            {
                NewItemText = "bad"
            };

            model.AddCommand.Execute().Subscribe();

            model.Error.ShouldBe("Invalid input");
            model.Items.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Clear_Error_When_Text_Changes()
        {
            var model = new TagInputModel([], text => text == "bad" ? "Invalid input" : null);

            model.NewItemText = "bad";
            model.AddCommand.Execute().Subscribe();
            model.Error.ShouldNotBeNull();

            model.NewItemText = "good";

            model.Error.ShouldBeNull();
        }
    }
}
