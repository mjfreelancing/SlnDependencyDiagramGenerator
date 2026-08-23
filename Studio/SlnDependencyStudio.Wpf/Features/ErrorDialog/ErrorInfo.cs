namespace SlnDependencyStudio.Wpf.Features.ErrorDialog;

/// <summary>Describes an error to be displayed to the user.</summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The error message body.</param>
public sealed record ErrorInfo(string Title, string Message);
