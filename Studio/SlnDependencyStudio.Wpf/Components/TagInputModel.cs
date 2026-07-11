using AllOverIt.Extensions;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Components;

/// <summary>Manages state for a single tag-input editor: a TextBox for adding items
/// plus an Add button, with items displayed as removable chips below.</summary>
public sealed class TagInputModel : ReactiveObject
{
    private readonly Func<string, string?>? _validateInput;
    private string _newItemText = string.Empty;
    private string? _error;

    /// <summary>The text currently entered in the add TextBox.</summary>
    public string NewItemText
    {
        get => _newItemText;
        set
        {
            this.RaiseAndSetIfChanged(ref _newItemText, value);
            Error = null;
        }
    }

    /// <summary>A validation error message to display, or <see langword="null"/> when input is valid.</summary>
    public string? Error
    {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }

    /// <summary>The collection of items managed by this tag input.</summary>
    public ObservableCollection<string> Items { get; }

    /// <summary>Command that adds the current <see cref="NewItemText"/> to <see cref="Items"/>.</summary>
    public ReactiveCommand<Unit, Unit> AddCommand { get; }

    /// <summary>Command that removes the specified item from <see cref="Items"/>.</summary>
    public ReactiveCommand<string, Unit> RemoveCommand { get; }

    /// <summary>Initializes a new instance of <see cref="TagInputModel"/> with no input validation.</summary>
    /// <param name="items">The backing collection from the editor.</param>
    public TagInputModel(ObservableCollection<string> items)
        : this(items, null)
    {
    }

    /// <summary>Initializes a new instance of <see cref="TagInputModel"/> with optional input validation.</summary>
    /// <param name="items">The backing collection from the editor.</param>
    /// <param name="validateInput">
    /// An optional validator. Return <see langword="null"/> when input is valid,
    /// or an error message string when validation fails.
    /// </param>
    public TagInputModel(ObservableCollection<string> items, Func<string, string?>? validateInput)
    {
        Items = items;
        _validateInput = validateInput;

        AddCommand = ReactiveCommand.Create(() =>
        {
            var text = NewItemText.Trim();

            if (text.IsNullOrEmpty())
            {
                return;
            }

            if (_validateInput is not null)
            {
                var validationError = _validateInput(text);

                if (validationError is not null)
                {
                    Error = validationError;
                    return;
                }
            }

            Items.Add(text);
            NewItemText = string.Empty;
        });

        RemoveCommand = ReactiveCommand.Create<string>(item => Items.Remove(item));
    }
}
