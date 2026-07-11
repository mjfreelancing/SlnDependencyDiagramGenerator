using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.Project.Stores;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Diagrams;

[Collection(nameof(ReactiveUIInitializer))]
public class DiagramsViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableCollection<DiagramFormat> _formats = new();
    private readonly IDiagramOptionsEditor _diagramOptionsEditor = Substitute.For<IDiagramOptionsEditor>();
    private readonly DiagramsViewModel _viewModel;

    public DiagramsViewModelFixture()
    {
        _diagramOptionsEditor.Formats.Returns(_formats);
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
                _viewModel.FormatToggles.ShouldContain(t => t.Format == format);
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

            viewModel.FormatToggles.Single(t => t.Format == DiagramFormat.D2).IsChecked.ShouldBeTrue();
            viewModel.FormatToggles.Single(t => t.Format == DiagramFormat.Mermaid).IsChecked.ShouldBeTrue();
        }

        [Fact]
        public void Should_Add_Format_When_Toggle_Checked()
        {
            var d2Toggle = _viewModel.FormatToggles.Single(t => t.Format == DiagramFormat.D2);

            d2Toggle.IsChecked = true;

            _formats.Items.ShouldContain(DiagramFormat.D2);
        }

        [Fact]
        public void Should_Remove_Format_When_Toggle_Unchecked()
        {
            _formats.Items.Add(DiagramFormat.Mermaid);

            var mermaidToggle = _viewModel.FormatToggles.Single(t => t.Format == DiagramFormat.Mermaid);
            mermaidToggle.IsChecked = false;

            _formats.Items.ShouldNotContain(DiagramFormat.Mermaid);
        }

        [Fact]
        public void Should_Sync_Toggle_When_Collection_Changes_Externally()
        {
            var d2Toggle = _viewModel.FormatToggles.Single(t => t.Format == DiagramFormat.D2);

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
    }
}
