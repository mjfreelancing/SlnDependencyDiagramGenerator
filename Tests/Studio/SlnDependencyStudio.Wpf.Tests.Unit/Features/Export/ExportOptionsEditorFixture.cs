using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Export;

[Collection(nameof(ReactiveUIInitializer))]
public class ExportOptionsEditorFixture : IDisposable
{
    private readonly ExportOptionsEditor _editor = new(Substitute.For<ILogger<ExportOptionsEditor>>());

    public class Construction : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Seed_RootPath_With_Empty_String()
        {
            _editor.RootPath.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Seed_UseRelativePath_With_True()
        {
            _editor.UseRelativePath.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Seed_ClearContents_With_False()
        {
            _editor.ClearContents.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Seed_ImageFormats_With_Empty_Collection()
        {
            _editor.ImageFormats.Items.ShouldBeEmpty();
        }
    }

    public class IsDirty : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_RootPath_Changes()
        {
            _editor.RootPath.Value = @"C:\Output";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_ClearContents_Changes()
        {
            _editor.SetOriginalValues(CreateOptions(rootPath: "", clearContents: false));

            _editor.ClearContents.Value = true;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_ImageFormat_Added()
        {
            _editor.ImageFormats.Items.Add(DiagramImageFormat.Png);

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_ImageFormat_Matches_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions(imageFormats: [DiagramImageFormat.Png]));

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Output"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Output"));

            _editor.RootPath.Value = @"C:\Other";

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(CreateOptions(@"C:\Other"));

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Load_ClearContents_From_Source()
        {
            _editor.SetOriginalValues(CreateOptions(clearContents: true));

            _editor.ClearContents.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Load_ImageFormats_From_Source()
        {
            _editor.SetOriginalValues(CreateOptions(imageFormats: [DiagramImageFormat.Png, DiagramImageFormat.Svg]));

            _editor.ImageFormats.Items.ShouldContain(DiagramImageFormat.Png);
            _editor.ImageFormats.Items.ShouldContain(DiagramImageFormat.Svg);
            _editor.ImageFormats.Items.Count.ShouldBe(2);
        }

        [Fact]
        public void Should_Sync_UseRelativePath_To_Absolute_When_RootPath_Is_Absolute()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Output"));

            _editor.RootPath.Value.ShouldBe(@"C:\Output");
            _editor.UseRelativePath.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Sync_UseRelativePath_To_Relative_When_RootPath_Is_Relative()
        {
            _editor.SetOriginalValues(CreateOptions(@"Output"));

            _editor.RootPath.Value.ShouldBe(@"Output");
            _editor.UseRelativePath.Value.ShouldBeTrue();
        }
    }

    public class FlushTo : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Write_Current_Values_To_Target()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Old"));

            _editor.RootPath.Value = @"C:\New";
            _editor.ClearContents.Value = true;
            _editor.ImageFormats.Items.Add(DiagramImageFormat.Svg);

            var target = new GeneratorExportOptions();

            _editor.FlushTo(target);

            target.RootPath.ShouldBe(@"C:\New");
            target.ClearContents.ShouldBeTrue();
            target.ImageFormats.ShouldBe([DiagramImageFormat.Svg]);
        }
    }

    public class UseRelativePathDirtyTracking : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Become_Dirty_And_Clean_When_UseRelativePath_Toggled()
        {
            const string documentDirectory = @"C:\docs";

            _editor.SetOriginalValues(CreateOptions(rootPath: @"Output", clearContents: false));
            _editor.IsDirty.ShouldBeFalse();

            // Toggling UseRelativePath OFF rewrites RootPath to its absolute form (mirrors the
            // ViewModel's WireRelativePathToggle). The editor should become dirty.
            _editor.UseRelativePath.Value = false;
            _editor.RootPath.Value = PathUtils.ResolveAsAbsolutePath(_editor.RootPath.Value, documentDirectory);
            _editor.IsDirty.ShouldBeTrue();

            // Toggling back ON rewrites RootPath to its relative baseline, so the editor becomes clean.
            _editor.UseRelativePath.Value = true;
            _editor.RootPath.Value = PathUtils.MakeRelativeIfPossible(
                PathUtils.ResolveAsAbsolutePath(_editor.RootPath.Value, documentDirectory), documentDirectory);

            _editor.RootPath.Value.ShouldBe(@"Output");
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Not_Be_Dirty_When_Toggle_Normalizes_Leading_Dot_Slash()
        {
            const string documentDirectory = @"C:\docs";

            // Baseline is the non-canonical relative form ".\Output".
            _editor.SetOriginalValues(CreateOptions(rootPath: @".\Output", clearContents: false));
            _editor.IsDirty.ShouldBeFalse();

            _editor.UseRelativePath.Value = false;
            _editor.RootPath.Value = PathUtils.ResolveAsAbsolutePath(_editor.RootPath.Value, documentDirectory);

            _editor.UseRelativePath.Value = true;
            _editor.RootPath.Value = PathUtils.MakeRelativeIfPossible(
                PathUtils.ResolveAsAbsolutePath(_editor.RootPath.Value, documentDirectory), documentDirectory);

            _editor.RootPath.Value.ShouldBe(@"Output");
            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class RelativePathSync : ExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Uncheck_UseRelativePath_When_RootPath_Becomes_Absolute()
        {
            _editor.SetOriginalValues(CreateOptions(@"Output"));

            _editor.RootPath.Value = @"C:\Output";

            _editor.UseRelativePath.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Check_UseRelativePath_When_RootPath_Becomes_Relative()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Output"));

            _editor.RootPath.Value = @"Output";

            _editor.UseRelativePath.Value.ShouldBeTrue();
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorExportOptions CreateOptions(
        string rootPath = "",
        bool clearContents = false,
        DiagramImageFormat[]? imageFormats = null)
    {
        return new GeneratorExportOptions
        {
            RootPath = rootPath,
            ClearContents = clearContents,
            ImageFormats = imageFormats ?? []
        };
    }
}
