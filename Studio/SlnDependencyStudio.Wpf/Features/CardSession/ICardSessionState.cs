namespace SlnDependencyStudio.Wpf.Features.CardSession;

/// <summary>Tracks the expanded/collapsed state of collapsible cards for the duration of a navigation session.
/// State is not persisted to disk.</summary>
public interface ICardSessionState
{
    /// <summary>Gets whether a card is currently expanded. Returns <see langword="true"/> (expanded)
    /// if no state has been stored yet for this card.</summary>
    /// <param name="cardKey">A unique identifier for the card (e.g. "Sources/SolutionPath").</param>
    /// <returns><see langword="true"/> if the card is expanded; <see langword="false"/> if collapsed.</returns>
    bool IsExpanded(string cardKey);

    /// <summary>Sets the expanded state for a card.</summary>
    /// <param name="cardKey">A unique identifier for the card.</param>
    /// <param name="isExpanded">The new expanded state.</param>
    void SetExpanded(string cardKey, bool isExpanded);
}
