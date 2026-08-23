using Shouldly;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Editors;

public class StudioEditorFactoryFixture
{
    private interface ITestEditor : IStudioEditor
    {
    }

    private sealed class TestEditor : ITestEditor
    {
    }

    public class CreateEditor : StudioEditorFactoryFixture
    {
        [Fact]
        public void Should_Resolve_Injected_Editor()
        {
            var factory = new StudioEditorFactory([new TestEditor()]);

            var editor = factory.CreateEditor<ITestEditor>();

            editor.ShouldBeOfType<TestEditor>();
        }

        [Fact]
        public void Should_Return_Same_Injected_Instance_On_Repeated_Calls()
        {
            var factory = new StudioEditorFactory([new TestEditor()]);

            var first = factory.CreateEditor<ITestEditor>();
            var second = factory.CreateEditor<ITestEditor>();

            first.ShouldBeSameAs(second);
        }
    }
}
