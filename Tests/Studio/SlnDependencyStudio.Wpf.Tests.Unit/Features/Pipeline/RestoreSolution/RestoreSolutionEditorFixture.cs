using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline.RestoreSolution;

[Collection(nameof(ReactiveUIInitializer))]
public class RestoreSolutionEditorFixture : IDisposable
{
    private readonly RestoreSolutionEditor _editor = new(Substitute.For<ILogger<RestoreSolutionEditor>>());

    public class Construction : RestoreSolutionEditorFixture
    {
        [Fact]
        public void Should_Seed_RestoreSolution_With_True()
        {
            _editor.RestoreSolution.Value.ShouldBeTrue();
        }
    }

    public class IsDirty : RestoreSolutionEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_Value_Changes()
        {
            _editor.RestoreSolution.Value = false;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(false);

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : RestoreSolutionEditorFixture
    {
        [Fact]
        public void Should_Set_Value()
        {
            _editor.SetOriginalValues(false);

            _editor.RestoreSolution.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(true);
            _editor.RestoreSolution.Value = false;

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(false);

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }
}
