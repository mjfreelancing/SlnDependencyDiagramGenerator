using AllOverIt.ReactiveUI;
using ReactiveUI;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Controls;

/// <summary>
/// Keeps a "use relative path" checkbox in sync with the path value it describes.
/// </summary>
internal static class RelativePathSync
{
    /// <summary>
    /// Debounce applied when re-deriving the checkbox from the path as it is typed, so the checkbox
    /// does not flicker while the user is mid-keystroke (e.g. while typing a "C:\" drive prefix).
    /// </summary>
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Subscribes to <paramref name="path"/> and updates <paramref name="useRelativePath"/> to
    /// reflect whether the current value is relative (<see langword="true"/>) or absolute. Used by
    /// the Solution, Export, and Pipeline editors so the checkbox always matches the stored path.
    /// This complements the immediate sync performed in the editor's <c>SetOriginalValues</c> (which
    /// covers document load) by also following subsequent edits/typing.
    /// </summary>
    /// <param name="path">The path trackable to observe (e.g. SolutionPath, RootPath, WorkingDirectory).</param>
    /// <param name="useRelativePath">The checkbox trackable to update.</param>
    public static IDisposable Wire(TrackableValue<string> path, TrackableValue<bool> useRelativePath)
    {
        return path
            .WhenAnyValue(value => value.Value)
            .Throttle(Delay, RxSchedulers.MainThreadScheduler)
            .Subscribe(value => useRelativePath.Value = !Path.IsPathFullyQualified(value));
    }
}
