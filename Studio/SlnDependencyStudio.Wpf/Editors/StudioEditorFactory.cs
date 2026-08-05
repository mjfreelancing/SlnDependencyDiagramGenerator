using Microsoft.Extensions.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Editors;

/// <summary>
/// Default implementation of <see cref="IStudioEditorFactory"/> that resolves editor instances from the container.
/// </summary>
internal sealed class StudioEditorFactory : IStudioEditorFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance of <see cref="StudioEditorFactory"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve editors.</param>
    public StudioEditorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public TEditor CreateEditor<TEditor>() where TEditor : IStudioEditor
    {
        return _serviceProvider.GetRequiredService<TEditor>();
    }
}
