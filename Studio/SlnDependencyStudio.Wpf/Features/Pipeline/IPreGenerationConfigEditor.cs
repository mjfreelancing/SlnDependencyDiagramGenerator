using SlnDependencyStudio.Wpf.Controls;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>
/// Read-only observable surface of the <see cref="PreGenerationConfig"/> editor wrapper.
/// </summary>
public interface IPreGenerationConfigEditor
{
    /// <summary><see langword="true"/> when any tracked value has diverged from its most recent baseline.</summary>
    bool IsDirty { get; }

    /// <summary>Whether the pre-generation command is enabled.</summary>
    TrackableValue<bool> Enabled { get; }

    /// <summary>The command or executable path to run.</summary>
    TrackableValue<string> Command { get; }

    /// <summary>Command-line arguments.</summary>
    TrackableValue<string> Arguments { get; }

    /// <summary>The working directory for the command.</summary>
    TrackableValue<string> WorkingDirectory { get; }

    /// <summary>Whether to continue on failure.</summary>
    TrackableValue<bool> ContinueOnFailure { get; }
}
