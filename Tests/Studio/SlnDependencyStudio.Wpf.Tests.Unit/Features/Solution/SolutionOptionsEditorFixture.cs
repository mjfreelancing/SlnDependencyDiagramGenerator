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

        [Fact]
        public void Should_Write_Scope_Values_To_Target()
        {
            var options = CreateOptions(@"C:\test.sln",
                individualEnabled: true, individualIncludeDeps: true, individualDepth: 3,
                allEnabled: false, allIncludeDeps: false, allDepth: 0);

            _editor.SetOriginalValues(options);

            _editor.IndividualEnabled.Value = false;
            _editor.IndividualIncludeDependencies.Value = false;
            _editor.IndividualTransitiveDepth.Value = 5;
            _editor.AllEnabled.Value = true;
            _editor.AllIncludeDependencies.Value = true;
            _editor.AllTransitiveDepth.Value = 7;

            var target = new GeneratorSolutionOptions();

            _editor.FlushTo(target);

            target.Individual.Enabled.ShouldBeFalse();
            target.Individual.IncludeDependencies.ShouldBeFalse();
            target.Individual.TransitiveDepth.ShouldBe(5);
            target.All.Enabled.ShouldBeTrue();
            target.All.IncludeDependencies.ShouldBeTrue();
            target.All.TransitiveDepth.ShouldBe(7);
        }
    }

    public class ScopeDirtyTracking : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Be_Dirty_When_IndividualEnabled_Changes()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\test.sln", individualEnabled: true));

            _editor.IndividualEnabled.Value = false;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_Dirty_When_AllTransitiveDepth_Changes()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\test.sln", allDepth: 2));

            _editor.AllTransitiveDepth.Value = 5;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Not_Be_Dirty_When_Scope_Values_Match()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\test.sln",
                individualEnabled: true, individualDepth: 3,
                allEnabled: false, allDepth: 0));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static GeneratorSolutionOptions CreateOptions(
        string solutionPath,
        bool individualEnabled = false,
        bool individualIncludeDeps = false,
        int individualDepth = 0,
        bool allEnabled = false,
        bool allIncludeDeps = false,
        int allDepth = 0)
    {
        return new GeneratorSolutionOptions
        {
            SolutionPath = solutionPath,
            Individual = new GeneratorSolutionOptions.ProjectScope
            {
                Enabled = individualEnabled,
                IncludeDependencies = individualIncludeDeps,
                TransitiveDepth = individualDepth
            },
            All = new GeneratorSolutionOptions.ProjectScope
            {
                Enabled = allEnabled,
                IncludeDependencies = allIncludeDeps,
                TransitiveDepth = allDepth
            }
        };
    }
}
