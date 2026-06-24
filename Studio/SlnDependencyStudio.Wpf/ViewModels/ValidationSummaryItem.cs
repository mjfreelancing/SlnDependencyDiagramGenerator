namespace SlnDependencyStudio.Wpf.ViewModels;

/// <summary>A single validation error or warning displayed in the floating validation summary bar.</summary>
public sealed class ValidationSummaryItem
{
    /// <summary>The error or warning message to display.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The type name of the view model that owns the field with the error.
    /// Used by the "jump to" link to navigate to the offending page.</summary>
    public Type? SourceViewModelType { get; init; }
}
