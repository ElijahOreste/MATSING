namespace MATSING.Models;

/// <summary>
/// Game modifiers that affect gameplay mechanics and difficulty.
/// Use [Flags] to allow combining multiple modifiers.
/// </summary>
[Flags]
public enum GameModifier
{
    /// <summary>
    /// No modifier. Standard gameplay.
    /// </summary>
    None = 0,

    /// <summary>
    /// Cards swap positions every 20-30 seconds, increasing chaos.
    /// </summary>
    CardDrift = 1 << 0,

    /// <summary>
    /// Cards gradually shrink as the game progresses.
    /// </summary>
    ShrinkingCards = 1 << 1,

    /// <summary>
    /// Match three cards instead of two per set.
    /// </summary>
    TripleMatch = 1 << 2,

    /// <summary>
    /// Each card can only be flipped a limited number of times before locking.
    /// </summary>
    FlipLimit = 1 << 3,

    /// <summary>
    /// Zen mode: no timer, no penalties, pure relaxation.
    /// </summary>
    ZenMode = 1 << 4,

    /// <summary>
    /// Hardcore mode: one wrong flip ends the game immediately.
    /// </summary>
    HardcoreMode = 1 << 5,

    /// <summary>
    /// Combo multiplier: consecutive matches build up a multiplier (×2, ×3, ×4...).
    /// </summary>
    ComboMultiplier = 1 << 6,
}
