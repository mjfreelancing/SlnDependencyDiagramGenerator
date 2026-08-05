using Microsoft.Extensions.DependencyInjection;
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
        public void Should_Resolve_Registered_Editor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestEditor, TestEditor>();
            using var provider = services.BuildServiceProvider();

            var factory = new StudioEditorFactory(provider);
            var editor = factory.CreateEditor<ITestEditor>();

            editor.ShouldBeOfType<TestEditor>();
        }

        [Fact]
        public void Should_Return_Same_Instance_For_Singleton_Registration()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestEditor, TestEditor>();
            using var provider = services.BuildServiceProvider();

            var factory = new StudioEditorFactory(provider);

            var first = factory.CreateEditor<ITestEditor>();
            var second = factory.CreateEditor<ITestEditor>();

            first.ShouldBeSameAs(second);
        }
    }
}
