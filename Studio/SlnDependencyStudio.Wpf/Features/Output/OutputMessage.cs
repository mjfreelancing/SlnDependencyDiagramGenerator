using ReactiveUI;

namespace SlnDependencyStudio.Wpf.Features.Output;

/// <summary>
/// A single message in the output panel. Level is used for color-coding.
/// </summary>
public sealed class OutputMessage : ReactiveObject
{
    private string _text = string.Empty;
    private OutputMessageLevel _level;

    /// <summary>The message text.</summary>
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    /// <summary>The severity level (Information, Warning, Error).</summary>
    public OutputMessageLevel Level
    {
        get => _level;
        set => this.RaiseAndSetIfChanged(ref _level, value);
    }
}
