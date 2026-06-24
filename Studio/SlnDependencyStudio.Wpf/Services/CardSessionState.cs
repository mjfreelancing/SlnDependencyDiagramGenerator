using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Services;

/// <summary>Default in-memory implementation of <see cref="ICardSessionState"/>.
/// Holds card expanded state in a <see cref="Dictionary{TKey,TValue}"/> for the lifetime
/// of the navigation session. All reads and writes happen on the UI thread — no concurrency
/// concerns. State is lost when the application exits.</summary>
internal sealed class CardSessionState : ICardSessionState, IStudioSingletonDependency
{
    private readonly Dictionary<string, bool> _expandedStates = [];

    /// <inheritdoc />
    public bool IsExpanded(string cardKey)
    {
        return !_expandedStates.TryGetValue(cardKey, out var isExpanded) || isExpanded;
    }

    /// <inheritdoc />
    public void SetExpanded(string cardKey, bool isExpanded)
    {
        _expandedStates[cardKey] = isExpanded;
    }
}
