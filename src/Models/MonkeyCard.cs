namespace MATSING.Models;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #3 — INHERITANCE
//  MonkeyCard EXTENDS CardBase.
//  It inherits all shared card state (CardId, Label, ImagePath,
//  IsFaceUp, IsMatched, FlipUp, FlipDown, MarkAsMatched) and
//  ADDS monkey-specific properties: CharacterName, Personality.
//  It provides CONCRETE implementations for the abstract members.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>INHERITANCE</b> — MonkeyCard extends <see cref="CardBase"/><br/>
/// Represents the standard monkey character card used in MATSING.
/// Inherits all base flip/match behaviour and adds character identity.
/// </summary>
public class MonkeyCard : CardBase
{
    // ── Monkey-Specific Properties (added by subclass) ────────────────────
    /// <summary>The monkey character's name (e.g. "Mehmet Bey").</summary>
    public string CharacterName { get; private set; }

    /// <summary>A one-word personality trait shown on the win screen.</summary>
    public string Personality { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────
    /// <summary>
    /// Creates a new MonkeyCard.
    /// </summary>
    /// <param name="id">Pair ID — two cards with the same ID form a match.</param>
    /// <param name="label">Short display label.</param>
    /// <param name="imagePath">Relative path to the card image.</param>
    /// <param name="characterName">Full character name.</param>
    /// <param name="personality">One-word personality descriptor.</param>
    public MonkeyCard(int id, string label, string imagePath,
                      string characterName, string personality)
    {
        // Inherited properties set here (protected set)
        CardId    = id;
        Label     = label;
        ImagePath = imagePath;

        // Subclass-specific
        CharacterName = characterName;
        Personality   = personality;
    }

    // ── Concrete Implementations of Abstract Members ──────────────────────

    /// <inheritdoc/>
    /// <remarks>Standard monkey cards are worth 100 points.</remarks>
    public override int PointValue => 100;

    /// <summary>
    /// <b>INHERITANCE (override)</b> — MonkeyCard's unique reveal action.<br/>
    /// Prints character info to debug output; the UI layer handles the visual animation.
    /// </summary>
    public override void Reveal()
    {
        System.Diagnostics.Debug.WriteLine(
            $"[MonkeyCard] {CharacterName} revealed! Personality: {Personality}");
    }

    /// <summary>
    /// Quick matches earn a 50-point bonus; slower matches earn 10.
    /// </summary>
    public override int CalculateBonus(int elapsedSeconds) =>
        elapsedSeconds < 30 ? 50 : 10;

    /// <summary>Adds personality info to the base ToString.</summary>
    public override string ToString() =>
        base.ToString() + $" Character={CharacterName} ({Personality})";
}
