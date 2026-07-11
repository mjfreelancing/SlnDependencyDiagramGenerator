using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Features.Diagrams;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Diagrams;

[Collection(nameof(ReactiveUIInitializer))]
public class DiagramOptionsEditorFixture : IDisposable
{
    private readonly DiagramOptionsEditor _editor = new();

    public class Construction : DiagramOptionsEditorFixture
    {
        [Fact]
        public void Should_Seed_Formats_With_Empty_Collection()
        {
            _editor.Formats.Items.ShouldNotBeNull();
            _editor.Formats.Items.ShouldBeEmpty();
        }
    }

    public class IsDirty : DiagramOptionsEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_Format_Added()
        {
            _editor.Formats.Items.Add(DiagramFormat.D2);

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_Format_Removed()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2, DiagramFormat.Mermaid]));

            _editor.IsDirty.ShouldBeFalse();

            _editor.Formats.Items.Remove(DiagramFormat.D2);

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_To_Clean_When_User_Toggles_Back_To_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.Formats.Items.Add(DiagramFormat.Mermaid);

            _editor.IsDirty.ShouldBeTrue();

            _editor.Formats.Items.Remove(DiagramFormat.Mermaid);

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Ignore_Order_When_Comparing_To_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.Mermaid, DiagramFormat.D2]));

            _editor.Formats.Items.Clear();

            // Simulate user toggling D2 first, then Mermaid — different order than baseline.
            _editor.Formats.Items.Add(DiagramFormat.D2);
            _editor.Formats.Items.Add(DiagramFormat.Mermaid);

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : DiagramOptionsEditorFixture
    {
        [Fact]
        public void Should_Populate_Formats_From_Source()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2, DiagramFormat.Mermaid]));

            _editor.Formats.Items.ShouldContain(DiagramFormat.D2);
            _editor.Formats.Items.ShouldContain(DiagramFormat.Mermaid);
        }

        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.Formats.Items.Add(DiagramFormat.Mermaid);

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2, DiagramFormat.Mermaid]));

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Clear_Previous_Formats()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2, DiagramFormat.Mermaid]));

            _editor.SetOriginalValues(CreateOptions([]));

            _editor.Formats.Items.ShouldBeEmpty();
        }
    }

    public class FlushTo : DiagramOptionsEditorFixture
    {
        [Fact]
        public void Should_Write_Current_Values_To_Target()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.Formats.Items.Add(DiagramFormat.Mermaid);

            var target = new GeneratorDiagramOptions();

            _editor.FlushTo(target);

            target.Formats.ShouldBe([DiagramFormat.D2, DiagramFormat.Mermaid]);
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorDiagramOptions CreateOptions(DiagramFormat[] formats)
    {
        return new GeneratorDiagramOptions
        {
            Formats = formats
        };
    }
}
