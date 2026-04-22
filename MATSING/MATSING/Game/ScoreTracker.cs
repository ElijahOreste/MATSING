namespace MATSING.Game;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #2 — ENCAPSULATION
//  ScoreTracker hides the scoring formula, streak counter, and
//  multipliers completely. External code can only READ Score
//  (public getter) and CALL the narrow public methods.
//  The _streak, _baseScore, and all constants are PRIVATE.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ENCAPSULATION</b> — ScoreTracker<br/>
/// Manages the player's score. All internal state (streak, base score,
/// multipliers) is private. Callers interact only through three methods
/// and one read-only property — they never touch the formula directly.
/// </summary>
public class ScoreTracker
{
    // ── Private State — fully hidden from outside ─────────────────────────
    private int _baseScore;
    private int _streak;

    private const int BASE_MATCH_POINTS   = 100;
    private const int STREAK_MULTIPLIER   = 25;
    private const int TIME_BONUS_FAST     = 50;   // < 30 s elapsed
    private const int TIME_BONUS_SLOW     = 10;

    // ── Public API — read-only score ──────────────────────────────────────
    /// <summary>
    /// Current player score. Can only be read from outside this class;
    /// modification is private to preserve encapsulation.
    /// </summary>
    public int Score { get; private set; }

    /// <summary>Current consecutive-match streak count.</summary>
    public int Streak => _streak;

    // ── Public Methods ────────────────────────────────────────────────────

    /// <summary>
    /// Registers a successful match and updates <see cref="Score"/>
    /// using the hidden internal formula.
    /// </summary>
    /// <param name="elapsedSeconds">Game time elapsed — faster = bigger bonus.</param>
    /// <param name="cardPoints">Base point value from the matched card type.</param>
    public void RegisterMatch(int elapsedSeconds, int cardPoints)
    {
        _streak++;
        int bonus     = elapsedSeconds < 30 ? TIME_BONUS_FAST : TIME_BONUS_SLOW;
        int streakPts = (_streak - 1) * STREAK_MULTIPLIER;
        _baseScore   += cardPoints;
        Score        += cardPoints + bonus + streakPts;
    }

    /// <summary>
    /// Registers a mismatch and resets the streak counter privately.
    /// </summary>
    public void RegisterMismatch()
    {
        _streak = 0;   // private state mutated internally
    }

    /// <summary>Resets all score state for a new game.</summary>
    public void Reset()
    {
        Score      = 0;
        _streak    = 0;
        _baseScore = 0;
    }
}
