using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Features.Solution.Models;

/// <summary>View model for a single tag-input editor: a TextBox for adding items
/// plus an Add button, with items displayed as removable chips below.</summary>
public sealed class TagInputModel : ReactiveObject
{
    private string _newItemText = string.Empty;

    /// <summary>The text currently entered in the add TextBox.</summary>
    public string NewItemText
    {
        get => _newItemText;
        set => this.RaiseAndSetIfChanged(ref _newItemText, value);
    }

    /// <summary>The collection of items managed by this tag input.</summary>
    public ObservableCollection<string> Items { get; }

    /// <summary>Command that adds the current <see cref="NewItemText"/> to <see cref="Items"/>.</summary>
    public ReactiveCommand<Unit, Unit> AddCommand { get; }

    /// <summary>Command that removes the specified item from <see cref="Items"/>.</summary>
    public ReactiveCommand<string, Unit> RemoveCommand { get; }

    /// <summary>Initializes a new instance of <see cref="TagInputModel"/>.</summary>
    /// <param name="items">The backing collection from the editor.</param>
    public TagInputModel(ObservableCollection<string> items)
    {
        Items = items;

        AddCommand = ReactiveCommand.Create(() =>
        {
            if (string.IsNullOrWhiteSpace(NewItemText))
            {
                return;
            }

            Items.Add(NewItemText.Trim());
            NewItemText = string.Empty;
        });

        RemoveCommand = ReactiveCommand.Create<string>(item => Items.Remove(item));
    }
}
