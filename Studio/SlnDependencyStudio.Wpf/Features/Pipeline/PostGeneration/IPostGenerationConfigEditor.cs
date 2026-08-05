using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;

/// <summary>
/// Observable editing surface of the <see cref="SlnDependencyStudio.Shared.Config.PostGenerationConfig"/> editor wrapper.
/// </summary>
public interface IPostGenerationConfigEditor : IStudioEditor, IStudioSingletonDependency
{
    /// <summary><see langword="true"/> when any tracked value has diverged from its most recent baseline.</summary>
    bool IsDirty { get; }

    /// <summary>Whether the post-generation command is enabled.</summary>
    TrackableValue<bool> Enabled { get; }

    /// <summary>The command or executable path to run.</summary>
    TrackableValue<string> Command { get; }

    /// <summary>Command-line arguments.</summary>
    TrackableValue<string> Arguments { get; }

    /// <summary>The working directory for the command.</summary>
    TrackableValue<string> WorkingDirectory { get; }

    /// <summary>Populates all TrackableValues from the given config and establishes a clean baseline.</summary>
    /// <param name="source">The post-generation config to load.</param>
    void SetOriginalValues(PostGenerationConfig source);

    /// <summary>Writes current TrackableValue contents back to the given config instance.</summary>
    /// <param name="target">The post-generation config to mutate.</param>
    void FlushTo(PostGenerationConfig target);
}
