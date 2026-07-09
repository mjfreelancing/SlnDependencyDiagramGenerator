using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Features.Solution;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Solution;

[Collection(nameof(ReactiveUIInitializer))]
public class SolutionOptionsEditorFixture : IDisposable
{
    private readonly SolutionOptionsEditor _editor = new();

    public class Construction : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Seed_SolutionPath_With_Empty_String()
        {
            _editor.SolutionPath.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Seed_UseRelativePath_With_True()
        {
            _editor.UseRelativePath.Value.ShouldBeTrue();
        }
    }

    public class IsDirty : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_SolutionPath_Changes()
        {
            _editor.SolutionPath.Value = @"C:\Projects\test.sln";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Projects\test.sln"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Projects\test.sln"));

            _editor.SolutionPath.Value = @"C:\Projects\other.sln";

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(CreateOptions(@"C:\Projects\other.sln"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class FlushTo : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Write_Current_Values_To_Target()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Old\path.sln"));

            _editor.SolutionPath.Value = @"C:\New\path.sln";

            var target = new GeneratorSolutionOptions();

            _editor.FlushTo(target);

            target.SolutionPath.ShouldBe(@"C:\New\path.sln");
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorSolutionOptions CreateOptions(string solutionPath)
    {
        return new GeneratorSolutionOptions
        {
            SolutionPath = solutionPath
        };
    }
}
