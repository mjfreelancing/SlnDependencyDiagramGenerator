using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Features.Export;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Export;

[Collection(nameof(ReactiveUIInitializer))]
public class GeneratorExportOptionsEditorFixture : IDisposable
{
    private readonly GeneratorExportOptionsEditor _editor = new();

    public class Construction : GeneratorExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Seed_RootPath_With_Empty_String()
        {
            _editor.RootPath.Value.ShouldBe(string.Empty);
        }
    }

    public class IsDirty : GeneratorExportOptionsEditorFixture
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
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Output"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : GeneratorExportOptionsEditorFixture
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
    }

    public class FlushTo : GeneratorExportOptionsEditorFixture
    {
        [Fact]
        public void Should_Write_Current_Values_To_Target()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Old"));

            _editor.RootPath.Value = @"C:\New";

            var target = new GeneratorExportOptions();

            _editor.FlushTo(target);

            target.RootPath.ShouldBe(@"C:\New");
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorExportOptions CreateOptions(string rootPath)
    {
        return new GeneratorExportOptions
        {
            RootPath = rootPath
        };
    }
}
