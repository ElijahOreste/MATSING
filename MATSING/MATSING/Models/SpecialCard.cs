namespace MATSING.Models;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #3 — INHERITANCE (second subclass)
//  SpecialCard also extends CardBase, demonstrating that the same
//  parent class can have MULTIPLE unrelated child classes.
//  Despite sharing the same CardBase parent as MonkeyCard, its
//  behaviour (PointValue=200, always-large bonus, flash reveal)
//  is completely different — which sets up Polymorphism.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>INHERITANCE</b> — SpecialCard extends <see cref="CardBase"/><br/>
/// A bonus variant card worth double points with a unique reveal effect.
/// Demonstrates that a single parent class can produce multiple,
/// behaviourally distinct child classes.
/// </summary>
public class SpecialCard : CardBase
{
    // ── SpecialCard-only property ─────────────────────────────────────────
    /// <summary>Describes the card's unique in-game ability text.</summary>
    public string SpecialAbility { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────
    /// <summary>Creates a new SpecialCard.</summary>
    /// <param name="id">Pair ID.</param>
    /// <param name="label">Display label.</param>
    /// <param name="imagePath">Image asset path.</param>
    /// <param name="ability">Text describing the card's special ability.</param>
    public SpecialCard(int id, string label, string imagePath, string ability)
    {
        CardId        = id;
        Label         = label;
        ImagePath     = imagePath;
        SpecialAbility = ability;
    }

    // ── Concrete Implementations ──────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>Special cards are worth DOUBLE (200 pts) compared to standard monkey cards.</remarks>
    public override int PointValue => 200;

    /// <summary>
    /// <b>INHERITANCE (override)</b> — SpecialCard's reveal triggers a screen flash
    /// rather than the standard bounce used by MonkeyCard.
    /// </summary>
    public override void Reveal()
    {
        System.Diagnostics.Debug.WriteLine(
            $"[SpecialCard] ⭐ Special card revealed! Ability: {SpecialAbility}");
    }

    /// <summary>
    /// Special cards always award a large time-bonus regardless of speed.
    /// </summary>
    public override int CalculateBonus(int elapsedSeconds) => 150;
}
