namespace MATSING.Utils;

/// <summary>
/// Static sound-effect player for MATSING.<br/>
/// Uses <see cref="System.Media.SoundPlayer"/> — no external libraries needed.<br/>
/// All sounds are fire-and-forget (non-blocking). Missing files are silently ignored.
/// </summary>
public static class SfxPlayer
{
    // ── Volume/mute toggle (can be wired to a settings toggle later) ──────
    public static bool Muted { get; set; } = false;

    // ── Cached players — loaded once, reused on every play ────────────────
    private static readonly System.Media.SoundPlayer? _flip       = Load("card_flip");
    private static readonly System.Media.SoundPlayer? _flipBack   = Load("card_flip_back");
    private static readonly System.Media.SoundPlayer? _match      = Load("match_found");
    private static readonly System.Media.SoundPlayer? _mismatch   = Load("mismatch");
    private static readonly System.Media.SoundPlayer? _flipLimit  = Load("fliplimit");
    private static readonly System.Media.SoundPlayer? _shuffle    = Load("shuffle");
    private static readonly System.Media.SoundPlayer? _gameWin    = Load("gamewin");
    private static readonly System.Media.SoundPlayer? _gameOver   = Load("gameover");
    private static readonly System.Media.SoundPlayer? _beep       = Load("coutndown_beep");
    private static readonly System.Media.SoundPlayer? _click      = Load("click");

    // Combo tier players indexed 0–3 → combo1–combo4
    private static readonly System.Media.SoundPlayer?[] _combo = new[]
    {
        Load("combo1"),
        Load("combo2"),
        Load("combo3"),
        Load("combo4"),
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Card flipped face-up.</summary>
    public static void PlayFlip()      => Play(_flip);

    /// <summary>Card flipped face-down (mismatch flip-back).</summary>
    public static void PlayFlipBack()  => Play(_flipBack);

    /// <summary>A matching pair (or triple) was found.</summary>
    public static void PlayMatch()     => Play(_match);

    /// <summary>Cards did not match.</summary>
    public static void PlayMismatch()  => Play(_mismatch);

    /// <summary>Player tried to flip a card that hit its FlipLimit.</summary>
    public static void PlayFlipLimit() => Play(_flipLimit);

    /// <summary>CardDrift shuffled the board.</summary>
    public static void PlayShuffle()   => Play(_shuffle);

    /// <summary>All pairs matched — game won!</summary>
    public static void PlayGameWin()   => Play(_gameWin);

    /// <summary>Time ran out or Hardcore game over.</summary>
    public static void PlayGameOver()  => Play(_gameOver);

    /// <summary>Low-time countdown beep (plays when timer bar turns red).</summary>
    public static void PlayCountdownBeep() => Play(_beep);

    /// <summary>Generic UI button click.</summary>
    public static void PlayClick()     => Play(_click);

    /// <summary>
    /// Combo hit. <paramref name="streakLevel"/> is the current streak (1-based).
    /// Plays combo1 for a 2× streak, combo2 for 3×, combo3 for 4×, combo4 for 5+.
    /// </summary>
    public static void PlayCombo(int streakLevel)
    {
        // streakLevel 1 = first match (no combo sound), 2+ = combo
        int idx = Math.Clamp(streakLevel - 2, 0, _combo.Length - 1);
        if (streakLevel >= 2)
            Play(_combo[idx]);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Loads a wav from Assets\Sfx\{name}.wav relative to the executable.
    /// Returns null (silently) if the file doesn't exist.
    /// </summary>
    private static System.Media.SoundPlayer? Load(string name)
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "Sfx", name + ".wav");

        if (!File.Exists(path)) return null;

        try
        {
            var player = new System.Media.SoundPlayer(path);
            player.Load(); // pre-buffer so first Play() has no latency
            return player;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Plays a player asynchronously. No-ops if null or muted.</summary>
    private static void Play(System.Media.SoundPlayer? player)
    {
        if (Muted || player == null) return;
        try { player.Play(); } catch { /* ignore playback errors */ }
    }
}
