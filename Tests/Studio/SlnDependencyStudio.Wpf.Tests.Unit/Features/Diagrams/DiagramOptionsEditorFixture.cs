using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Diagrams;

[Collection(nameof(ReactiveUIInitializer))]
public class DiagramOptionsEditorFixture : IDisposable
{
    private readonly DiagramOptionsEditor _editor = new(Substitute.For<ILogger<DiagramOptionsEditor>>());

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
        public void Should_Populate_All_Styling_Fields_From_Source()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2],
                direction: GeneratorDiagramOptions.DiagramDirection.BT,
                frameworkFill: "#111111", frameworkOpacity: 0.1,
                packageFill: "#222222", packageOpacity: 0.2,
                transitiveFill: "#333333", transitiveOpacity: 0.3,
                groupingEnabled: false,
                groupingFill: "#444444", groupingOpacity: 0.4,
                groupName: "TestGroup", groupNameAlias: "tg"));

            _editor.Direction.Value.ShouldBe(GeneratorDiagramOptions.DiagramDirection.BT);
            _editor.FrameworkFill.Value.ShouldBe("#111111");
            _editor.FrameworkOpacity.Value.ShouldBe(0.1);
            _editor.PackageFill.Value.ShouldBe("#222222");
            _editor.PackageOpacity.Value.ShouldBe(0.2);
            _editor.TransitiveFill.Value.ShouldBe("#333333");
            _editor.TransitiveOpacity.Value.ShouldBe(0.3);
            _editor.GroupingEnabled.Value.ShouldBeFalse();
            _editor.GroupingFill.Value.ShouldBe("#444444");
            _editor.GroupingOpacity.Value.ShouldBe(0.4);
            _editor.GroupName.Value.ShouldBe("TestGroup");
            _editor.GroupNameAlias.Value.ShouldBe("tg");
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

        [Fact]
        public void Should_Write_All_Styling_Fields_To_Target()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.PackageFill.Value = "#AAA";
            _editor.PackageOpacity.Value = 0.5;
            _editor.TransitiveFill.Value = "#BBB";
            _editor.TransitiveOpacity.Value = 0.6;
            _editor.GroupingEnabled.Value = false;
            _editor.GroupingFill.Value = "#CCC";
            _editor.GroupingOpacity.Value = 0.7;
            _editor.GroupName.Value = "G";
            _editor.GroupNameAlias.Value = "g";

            var target = new GeneratorDiagramOptions();

            _editor.FlushTo(target);

            target.PackageStyle.Fill.ShouldBe("#AAA");
            target.PackageStyle.Opacity.ShouldBe(0.5);
            target.TransitiveStyle.Fill.ShouldBe("#BBB");
            target.TransitiveStyle.Opacity.ShouldBe(0.6);
            target.Grouping.Enabled.ShouldBeFalse();
            target.Grouping.BackgroundStyle.Fill.ShouldBe("#CCC");
            target.Grouping.BackgroundStyle.Opacity.ShouldBe(0.7);
            target.GroupName.ShouldBe("G");
            target.GroupNameAlias.ShouldBe("g");
        }
    }

    public class StylingDirtyTracking : DiagramOptionsEditorFixture
    {
        [Fact]
        public void Should_Be_Dirty_When_Direction_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2]));

            _editor.Direction.Value = GeneratorDiagramOptions.DiagramDirection.BT;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Dirty_When_Fill_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2], frameworkFill: "#FF0000"));

            _editor.FrameworkFill.Value = "#0000FF";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Dirty_When_GroupingEnabled_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2], groupingEnabled: true));

            _editor.GroupingEnabled.Value = false;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Be_Dirty_When_Styles_Match()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2],
                direction: GeneratorDiagramOptions.DiagramDirection.RL,
                frameworkFill: "#FF0000",
                frameworkOpacity: 0.5));

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_Dirty_When_PackageFill_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2], packageFill: "#FF0000"));

            _editor.PackageFill.Value = "#0000FF";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Dirty_When_TransitiveOpacity_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2], transitiveOpacity: 0.5));

            _editor.TransitiveOpacity.Value = 0.9;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Dirty_When_GroupName_Changes()
        {
            _editor.SetOriginalValues(CreateOptions([DiagramFormat.D2], groupName: "Old"));

            _editor.GroupName.Value = "New";

            _editor.IsDirty.ShouldBeTrue();
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorDiagramOptions CreateOptions(
        DiagramFormat[] formats,
        GeneratorDiagramOptions.DiagramDirection direction = GeneratorDiagramOptions.DiagramDirection.LR,
        string frameworkFill = "",
        double frameworkOpacity = 1.0,
        string packageFill = "",
        double packageOpacity = 1.0,
        string transitiveFill = "",
        double transitiveOpacity = 1.0,
        bool groupingEnabled = true,
        string groupingFill = "#E7EBFC",
        double groupingOpacity = 1.0,
        string groupName = "",
        string groupNameAlias = "")
    {
        return new GeneratorDiagramOptions
        {
            Formats = formats,
            Direction = direction,
            FrameworkStyle = new GeneratorDiagramOptions.FillStyle { Fill = frameworkFill, Opacity = frameworkOpacity },
            PackageStyle = new GeneratorDiagramOptions.FillStyle { Fill = packageFill, Opacity = packageOpacity },
            TransitiveStyle = new GeneratorDiagramOptions.FillStyle { Fill = transitiveFill, Opacity = transitiveOpacity },
            Grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                Enabled = groupingEnabled,
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle { Fill = groupingFill, Opacity = groupingOpacity }
            },
            GroupName = groupName,
            GroupNameAlias = groupNameAlias
        };
    }
}
