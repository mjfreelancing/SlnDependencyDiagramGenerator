using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Diagrams;

[Collection(nameof(ReactiveUIInitializer))]
public class DiagramsViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableCollection<DiagramFormat> _formats = new();
    private readonly TrackableValue<string> _frameworkFill = new();
    private readonly TrackableValue<string> _packageFill = new();
    private readonly TrackableValue<string> _transitiveFill = new();
    private readonly TrackableValue<string> _groupingFill = new();
    private readonly IDiagramOptionsEditor _diagramOptionsEditor = Substitute.For<IDiagramOptionsEditor>();
    private readonly DiagramsViewModel _viewModel;

    public DiagramsViewModelFixture()
    {
        _frameworkFill.SetOriginalValue(string.Empty);
        _packageFill.SetOriginalValue(string.Empty);
        _transitiveFill.SetOriginalValue(string.Empty);
        _groupingFill.SetOriginalValue(string.Empty);

        _diagramOptionsEditor.Formats.Returns(_formats);
        _diagramOptionsEditor.FrameworkFill.Returns(_frameworkFill);
        _diagramOptionsEditor.PackageFill.Returns(_packageFill);
        _diagramOptionsEditor.TransitiveFill.Returns(_transitiveFill);
        _diagramOptionsEditor.GroupingFill.Returns(_groupingFill);
        _store.DiagramOptionsEditor.Returns(_diagramOptionsEditor);

        _viewModel = new DiagramsViewModel(_store);
    }

    public class Construction : DiagramsViewModelFixture
    {
        [Fact]
        public void Should_Expose_Formats_From_Store_Editor()
        {
            _viewModel.Formats.ShouldBeSameAs(_formats);
        }

        [Fact]
        public void Should_Have_FormatToggles_For_All_Enum_Values()
        {
            var expectedFormats = Enum.GetValues<DiagramFormat>();

            _viewModel.FormatToggles.Count.ShouldBe(expectedFormats.Length);

            foreach (var format in expectedFormats)
            {
                _viewModel.FormatToggles.ShouldContain(toggle => toggle.Format == format);
            }
        }

        [Fact]
        public void Should_Have_ValidationContext()
        {
            _viewModel.ValidationContext.ShouldNotBeNull();
        }
    }

    public class FormatToggleSync : DiagramsViewModelFixture
    {
        [Fact]
        public void Should_Sync_Toggles_On_Construction_When_Formats_Already_Populated()
        {
            // Simulate document already loaded before ViewModel is created.
            _formats.Items.Add(DiagramFormat.D2);
            _formats.Items.Add(DiagramFormat.Mermaid);

            var viewModel = new DiagramsViewModel(_store);

            viewModel.FormatToggles.Single(toggle => toggle.Format == DiagramFormat.D2).IsChecked.ShouldBeTrue();
            viewModel.FormatToggles.Single(toggle => toggle.Format == DiagramFormat.Mermaid).IsChecked.ShouldBeTrue();
        }

        [Fact]
        public void Should_Add_Format_When_Toggle_Checked()
        {
            var d2Toggle = _viewModel.FormatToggles.Single(toggle => toggle.Format == DiagramFormat.D2);

            d2Toggle.IsChecked = true;

            _formats.Items.ShouldContain(DiagramFormat.D2);
        }

        [Fact]
        public void Should_Remove_Format_When_Toggle_Unchecked()
        {
            _formats.Items.Add(DiagramFormat.Mermaid);

            var mermaidToggle = _viewModel.FormatToggles.Single(toggle => toggle.Format == DiagramFormat.Mermaid);
            mermaidToggle.IsChecked = false;

            _formats.Items.ShouldNotContain(DiagramFormat.Mermaid);
        }

        [Fact]
        public void Should_Sync_Toggle_When_Collection_Changes_Externally()
        {
            var d2Toggle = _viewModel.FormatToggles.Single(toggle => toggle.Format == DiagramFormat.D2);

            _formats.Items.Add(DiagramFormat.D2);

            d2Toggle.IsChecked.ShouldBeTrue();
        }

        [Fact]
        public void Should_Sync_All_Toggles_When_Collection_Cleared()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _formats.Items.Add(DiagramFormat.Mermaid);

            _formats.Items.Clear();

            foreach (var toggle in _viewModel.FormatToggles)
            {
                toggle.IsChecked.ShouldBeFalse();
            }
        }
    }

    public class Validation : DiagramsViewModelFixture
    {
        [Fact]
        public void Should_Fail_When_No_Format_Selected()
        {
            _formats.Items.Clear();

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_At_Least_One_Format_Selected()
        {
            _formats.Items.Add(DiagramFormat.D2);

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Fail_When_Hex_Is_Invalid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "invalid";

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_Hex_Is_Valid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "#FF0000";

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Fail_When_PackageFill_Is_Invalid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _packageFill.Value = "bad";

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Fail_When_TransitiveFill_Is_Invalid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _transitiveFill.Value = "xyz";

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Fail_When_GroupingFill_Is_Invalid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _groupingFill.Value = "not-a-color";

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_Hex_Has_No_Hash_Prefix()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "FF0000";

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }
    }

    public class HexError : DiagramsViewModelFixture
    {
        [Fact]
        public void Should_Return_Null_When_All_Fills_Valid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "#FF0000";
            _packageFill.Value = "#00FF00";
            _transitiveFill.Value = "#0000FF";
            _groupingFill.Value = "#FFFFFF";

            _viewModel.StylesHexError.ShouldBeNull();
            _viewModel.GroupingHexError.ShouldBeNull();
        }

        [Fact]
        public void Should_Return_Framework_Error_First()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "bad";
            _packageFill.Value = "also-bad";

            _viewModel.StylesHexError!.ShouldContain("Framework");
        }

        [Fact]
        public void Should_Return_Package_Error_When_Framework_Valid()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "#FF0000";
            _packageFill.Value = "bad";

            _viewModel.StylesHexError!.ShouldContain("Package");
        }

        [Fact]
        public void Should_Return_Grouping_Error_Separately()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _groupingFill.Value = "bad";

            _viewModel.GroupingHexError!.ShouldContain("Grouping");
            _viewModel.StylesHexError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_Error_When_Fixed()
        {
            _formats.Items.Add(DiagramFormat.D2);
            _frameworkFill.Value = "bad";

            _viewModel.StylesHexError.ShouldNotBeNull();

            _frameworkFill.Value = "#FF0000";

            _viewModel.StylesHexError.ShouldBeNull();
        }

        [Fact]
        public void Should_Cascade_Through_All_Errors_As_Each_Is_Fixed()
        {
            _formats.Items.Add(DiagramFormat.D2);

            // All four fills invalid.
            _frameworkFill.Value = "bad1";
            _packageFill.Value = "bad2";
            _transitiveFill.Value = "bad3";
            _groupingFill.Value = "bad4";

            _viewModel.StylesHexError!.ShouldContain("Framework");
            _viewModel.GroupingHexError!.ShouldContain("Grouping");

            // Fix Framework → Package error surfaces.
            _frameworkFill.Value = "#FF0000";
            _viewModel.StylesHexError!.ShouldContain("Package");
            _viewModel.GroupingHexError!.ShouldContain("Grouping");

            // Fix Package → Transitive error surfaces.
            _packageFill.Value = "#00FF00";
            _viewModel.StylesHexError!.ShouldContain("Transitive");
            _viewModel.GroupingHexError!.ShouldContain("Grouping");

            // Fix Transitive → Styles clean, Grouping still has error.
            _transitiveFill.Value = "#0000FF";
            _viewModel.StylesHexError.ShouldBeNull();
            _viewModel.GroupingHexError!.ShouldContain("Grouping");

            // Fix Grouping → all clean.
            _groupingFill.Value = "#FFFFFF";
            _viewModel.StylesHexError.ShouldBeNull();
            _viewModel.GroupingHexError.ShouldBeNull();
        }
    }
}
