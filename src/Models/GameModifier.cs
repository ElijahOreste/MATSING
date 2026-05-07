namespace MATSING.Models;

/// <summary>
/// Game modifiers that affect gameplay mechanics and difficulty.
/// Use [Flags] to allow combining multiple modifiers.
/// </summary>
[Flags]
public enum GameModifier
{
    /// <summary>No modifier. Standard gameplay.</summary>
    None = 0,

    /// <summary>Cards swap positions every 20s, increasing chaos.</summary>
    CardDrift = 1 << 0,

    /// <summary>Cards gradually shrink as the game progresses.</summary>
    ShrinkingCards = 1 << 1,

    /// <summary>
    /// Memory Leak Mode: extra decoy cards are injected into the board that look
    /// like real cards but have no match. They disappear after being flipped once,
    /// wasting a turn and resetting the streak.
    /// </summary>
    MemoryLeakMode = 1 << 2,

    /// <summary>
    /// Ghost Card Mode: when a card is flipped there is a 40% chance it shows
    /// a blank face instead of its real image for 600ms before revealing,
    /// making identification confusing.
    /// </summary>
    GhostCard = 1 << 3,

    /// <summary>Zen mode: no timer, no penalties, pure relaxation.</summary>
    ZenMode = 1 << 4,

    /// <summary>
    /// Hardcore Mode: player gets a limited number of hearts (3 on Easy,
    /// 4 on Medium, 5 on Hard). Each mismatch costs one heart. Losing all
    /// hearts ends the game immediately.
    /// </summary>
    HardcoreMode = 1 << 5,

    /// <summary>Combo multiplier: consecutive matches build up a multiplier (×2, ×3, ×4).</summary>
    ComboMultiplier = 1 << 6,

    /// <summary>
    /// Endless Mode: when all pairs on the current board are matched, a fresh
    /// shuffled board is automatically dealt and play continues. Score accumulates
    /// across boards. Only ends when time runs out (or never in Zen+Endless).
    /// </summary>
    EndlessMode = 1 << 7,
}