namespace MATSING.Game;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #2 — ENCAPSULATION
//  ScoreTracker hides the scoring formula, streak counter, and
//  multipliers completely. External code can only READ Score
//  (public getter) and CALL the narrow public methods.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ENCAPSULATION</b> — ScoreTracker<br/>
/// Manages the player's score. All internal state (streak, base score,
/// multipliers) is private. Callers interact only through three methods
/// and one read-only property — they never touch the formula directly.
/// </summary>
public class ScoreTracker
{
    // ── Private State ─────────────────────────────────────────────────────
    private int  _baseScore;
    private int  _streak;
    private bool _comboMode;

    private const int STREAK_MULTIPLIER = 25;
    private const int TIME_BONUS_FAST   = 50;   // < 30 s elapsed
    private const int TIME_BONUS_SLOW   = 10;

    // Max combo multiplier caps at ×4
    private const int MAX_COMBO = 4;

    // ── Public API — read-only score ──────────────────────────────────────
    /// <summary>Current player score. Read-only outside this class.</summary>
    public int Score { get; private set; }

    /// <summary>Current consecutive-match streak count.</summary>
    public int Streak => _streak;

    /// <summary>
    /// Current combo multiplier (1–4). Only meaningful when ComboMultiplier
    /// modifier is active; otherwise always 1.
    /// </summary>
    public int ComboMultiplier => _comboMode ? Math.Min(_streak, MAX_COMBO) : 1;

    // ── Public Methods ────────────────────────────────────────────────────

    /// <summary>
    /// Registers a successful match and updates <see cref="Score"/>
    /// using the hidden internal formula.
    /// </summary>
    public void RegisterMatch(int elapsedSeconds, int cardPoints)
    {
        _streak++;
        int bonus     = elapsedSeconds < 30 ? TIME_BONUS_FAST : TIME_BONUS_SLOW;
        int streakPts = (_streak - 1) * STREAK_MULTIPLIER;
        _baseScore   += cardPoints;

        if (_comboMode)
        {
            // ComboMultiplier: multiply the entire reward by the capped streak
            int multiplier = Math.Min(_streak, MAX_COMBO);
            Score += (cardPoints + bonus + streakPts) * multiplier;
        }
        else
        {
            Score += cardPoints + bonus + streakPts;
        }
    }

    /// <summary>Registers a mismatch and resets the streak counter.</summary>
    public void RegisterMismatch()
    {
        _streak = 0;
    }

    /// <summary>Resets all score state for a new game.</summary>
    public void Reset(bool comboMode = false)
    {
        Score      = 0;
        _streak    = 0;
        _baseScore = 0;
        _comboMode = comboMode;
    }
}