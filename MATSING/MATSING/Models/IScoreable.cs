namespace MATSING.Models;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #1 — ABSTRACTION (continued)
//  IScoreable abstracts the concept of "something that has score value".
//  The scoring formula is hidden behind this interface.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ABSTRACTION</b> — Interface IScoreable<br/>
/// Any game entity that contributes to the player's score must
/// implement this interface. The internal bonus formula is hidden;
/// callers simply ask for <see cref="CalculateBonus"/>.
/// </summary>
public interface IScoreable
{
    /// <summary>Base point value awarded when this card is matched.</summary>
    int PointValue { get; }

    /// <summary>
    /// Calculates a time-based bonus for matching this card.
    /// </summary>
    /// <param name="elapsedSeconds">Seconds elapsed since the game started.</param>
    /// <returns>Bonus points to add to the player's score.</returns>
    int CalculateBonus(int elapsedSeconds);
}
