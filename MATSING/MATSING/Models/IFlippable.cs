namespace MATSING.Models;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #1 — ABSTRACTION
//  Interface IFlippable defines the CONTRACT for any flippable card.
//  Callers never need to know HOW flipping is implemented —
//  they simply call FlipUp() / FlipDown() on any IFlippable.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ABSTRACTION</b> — Interface IFlippable<br/>
/// Defines the minimum contract that any card object must fulfil
/// to participate in the game's flip mechanic.<br/>
/// The implementation detail of HOW a flip works is completely hidden
/// from every consumer; they only see this surface.
/// </summary>
public interface IFlippable
{
    /// <summary>Gets whether the card is currently face-up.</summary>
    bool IsFaceUp { get; }

    /// <summary>Gets whether this card has already been matched.</summary>
    bool IsMatched { get; }

    /// <summary>Turns the card face-up.</summary>
    void FlipUp();

    /// <summary>Turns the card face-down.</summary>
    void FlipDown();

    /// <summary>Permanently marks this card as matched (cannot be flipped back).</summary>
    void MarkAsMatched();
}
