namespace MATSING.Models;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #1 — ABSTRACTION
//  CardBase is an abstract class. It cannot be instantiated directly.
//  It provides the shared template (state + default behaviour) for
//  ALL card types, while leaving Reveal() and CalculateBonus()
//  as abstract — forcing each subclass to supply its own logic.
//
//  AOOP PRINCIPLE #2 — ENCAPSULATION (partial)
//  Internal state (IsFaceUp, IsMatched) uses protected setters so
//  subclasses can update them, but external code cannot.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ABSTRACTION</b> — Abstract Base Class CardBase<br/>
/// Defines the shared template for ALL card types in MATSING.
/// <list type="bullet">
///   <item><description><see cref="Reveal"/> is <c>abstract</c> — each subclass defines its own visual effect.</description></item>
///   <item><description><see cref="CalculateBonus"/> is <c>abstract</c> — scoring formula differs per card type.</description></item>
///   <item><description>Consumers can work with <c>CardBase</c> references without knowing the concrete type.</description></item>
/// </list>
/// </summary>
public abstract class CardBase : IFlippable, IScoreable
{
    // ── Shared Identity ──────────────────────────────────────────────────
    /// <summary>Unique identifier that links two cards of the same pair.</summary>
    public int CardId { get; protected set; }

    /// <summary>Short display label shown on the card front (e.g. "CEO Mode").</summary>
    public string Label { get; protected set; } = string.Empty;

    /// <summary>Relative file path to the card's image asset.</summary>
    public string ImagePath { get; protected set; } = string.Empty;

    // ── IFlippable state (protected set = Encapsulation) ─────────────────
    /// <inheritdoc/>
    public bool IsFaceUp { get; protected set; }

    /// <inheritdoc/>
    public bool IsMatched { get; protected set; }

    // ── Abstract Members (Abstraction) ───────────────────────────────────

    /// <summary>
    /// <b>ABSTRACTION</b> — abstract property.<br/>
    /// Each card type declares its own base point value.
    /// </summary>
    public abstract int PointValue { get; }

    /// <summary>
    /// <b>ABSTRACTION</b> — abstract method.<br/>
    /// Triggers the card's unique reveal behaviour (animation hook).
    /// Subclasses decide HOW the reveal looks; callers just call Reveal().
    /// </summary>
    public abstract void Reveal();

    /// <inheritdoc/>
    public abstract int CalculateBonus(int elapsedSeconds);

    // ── Default IFlippable Implementations (virtual = overridable) ───────

    /// <summary>Flips card face-up and fires the <see cref="OnFlipped"/> hook.</summary>
    public virtual void FlipUp()
    {
        IsFaceUp = true;
        OnFlipped();
    }

    /// <summary>Flips card face-down.</summary>
    public virtual void FlipDown()
    {
        IsFaceUp = false;
    }

    /// <summary>Permanently marks this card as matched.</summary>
    public void MarkAsMatched()
    {
        IsMatched = true;
        IsFaceUp  = true;
    }

    // ── Protected Extension Hook ──────────────────────────────────────────
    /// <summary>
    /// Called by <see cref="FlipUp"/> after state is updated.
    /// Subclasses may override this to add extra behaviour (e.g. sound).
    /// </summary>
    protected virtual void OnFlipped() { /* optional hook for subclasses */ }

    /// <inheritdoc/>
    public override string ToString() =>
        $"[{GetType().Name}] Id={CardId} Label={Label} FaceUp={IsFaceUp} Matched={IsMatched}";
}
