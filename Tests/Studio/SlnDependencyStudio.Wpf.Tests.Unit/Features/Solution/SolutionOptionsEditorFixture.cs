using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Features.Solution;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Solution;

[Collection(nameof(ReactiveUIInitializer))]
public class SolutionOptionsEditorFixture : IDisposable
{
    private readonly SolutionOptionsEditor _editor = new(Substitute.For<ILogger<SolutionOptionsEditor>>());

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

        [Fact]
        public void Should_Sync_UseRelativePath_To_Absolute_When_SolutionPath_Is_Absolute()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Projects\test.sln"));

            _editor.SolutionPath.Value.ShouldBe(@"C:\Projects\test.sln");
            _editor.UseRelativePath.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Sync_UseRelativePath_To_Relative_When_SolutionPath_Is_Relative()
        {
            _editor.SetOriginalValues(CreateOptions(@"..\Projects\test.sln"));

            _editor.SolutionPath.Value.ShouldBe(@"..\Projects\test.sln");
            _editor.UseRelativePath.Value.ShouldBeTrue();
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

    public class UseRelativePathDirtyTracking : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Become_Dirty_And_Clean_When_UseRelativePath_Toggled()
        {
            const string documentDirectory = @"C:\docs";

            _editor.SetOriginalValues(CreateOptions(@"Output\MyApp.sln"));
            _editor.IsDirty.ShouldBeFalse();

            // Toggling UseRelativePath OFF rewrites SolutionPath to its absolute form (mirrors the
            // ViewModel's WireRelativePathToggle). The editor should become dirty.
            _editor.UseRelativePath.Value = false;
            _editor.SolutionPath.Value = PathUtils.ResolveAsAbsolutePath(_editor.SolutionPath.Value, documentDirectory);
            _editor.IsDirty.ShouldBeTrue();

            // Toggling back ON rewrites SolutionPath to its relative baseline, so the editor becomes clean.
            _editor.UseRelativePath.Value = true;
            _editor.SolutionPath.Value = PathUtils.MakeRelativeIfPossible(
                PathUtils.ResolveAsAbsolutePath(_editor.SolutionPath.Value, documentDirectory), documentDirectory);

            _editor.SolutionPath.Value.ShouldBe(@"Output\MyApp.sln");
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Not_Be_Dirty_When_Toggle_Normalizes_Leading_Dot_Slash()
        {
            const string documentDirectory = @"C:\docs";

            // Baseline is the non-canonical relative form ".\Output\MyApp.sln".
            _editor.SetOriginalValues(CreateOptions(@".\Output\MyApp.sln"));
            _editor.IsDirty.ShouldBeFalse();

            _editor.UseRelativePath.Value = false;
            _editor.SolutionPath.Value = PathUtils.ResolveAsAbsolutePath(_editor.SolutionPath.Value, documentDirectory);

            _editor.UseRelativePath.Value = true;
            _editor.SolutionPath.Value = PathUtils.MakeRelativeIfPossible(
                PathUtils.ResolveAsAbsolutePath(_editor.SolutionPath.Value, documentDirectory), documentDirectory);

            _editor.SolutionPath.Value.ShouldBe(@"Output\MyApp.sln");
            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class RelativePathSync : SolutionOptionsEditorFixture
    {
        [Fact]
        public void Should_Uncheck_UseRelativePath_When_SolutionPath_Becomes_Absolute()
        {
            _editor.SetOriginalValues(CreateOptions(@"..\Projects\test.sln"));

            _editor.SolutionPath.Value = @"C:\Projects\test.sln";

            _editor.UseRelativePath.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Check_UseRelativePath_When_SolutionPath_Becomes_Relative()
        {
            _editor.SetOriginalValues(CreateOptions(@"C:\Projects\test.sln"));

            _editor.SolutionPath.Value = @"..\Projects\test.sln";

            _editor.UseRelativePath.Value.ShouldBeTrue();
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
