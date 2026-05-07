namespace MATSING.Utils;

/// <summary>
/// Static sound-effect player for MATSING.
/// Uses <see cref="System.Media.SoundPlayer"/> — no external libraries needed.
/// 
/// FIX: The original cached a single SoundPlayer per sound and called .Play() (async).
/// When two sounds fired rapidly (e.g. flip then immediate mismatch), the second
/// .Play() on the same instance cancelled the first mid-playback.
/// Fix: each Play() creates a fresh SoundPlayer from the cached stream bytes,
/// so sounds never step on each other.
/// </summary>
public static class SfxPlayer
{
    public static bool Muted { get; set; } = false;

    // Pre-read all wav files into byte arrays once at startup.
    // Creating a SoundPlayer from a byte[] is cheap; creating from file is not.
    private static readonly byte[]? _flip       = LoadBytes("card_flip");
    private static readonly byte[]? _flipBack   = LoadBytes("card_flip_back");
    private static readonly byte[]? _match      = LoadBytes("match_found");
    private static readonly byte[]? _mismatch   = LoadBytes("mismatch");
    private static readonly byte[]? _flipLimit  = LoadBytes("fliplimit");
    private static readonly byte[]? _shuffle    = LoadBytes("shuffle");
    private static readonly byte[]? _gameWin    = LoadBytes("gamewin");
    private static readonly byte[]? _gameOver   = LoadBytes("gameover");
    private static readonly byte[]? _beep       = LoadBytes("coutndown_beep");
    private static readonly byte[]? _click      = LoadBytes("click");

    private static readonly byte[]?[] _combo =
    {
        LoadBytes("combo1"),
        LoadBytes("combo2"),
        LoadBytes("combo3"),
        LoadBytes("combo4"),
    };

    // ── Public API ────────────────────────────────────────────────────────

    public static void PlayFlip()           => Play(_flip);
    public static void PlayFlipBack()       => Play(_flipBack);
    public static void PlayMatch()          => Play(_match);
    public static void PlayMismatch()       => Play(_mismatch);
    public static void PlayFlipLimit()      => Play(_flipLimit);
    public static void PlayShuffle()        => Play(_shuffle);
    public static void PlayGameWin()        => Play(_gameWin);
    public static void PlayGameOver()       => Play(_gameOver);
    public static void PlayCountdownBeep()  => Play(_beep);
    public static void PlayClick()          => Play(_click);

    /// <summary>
    /// Plays the appropriate combo tier.
    /// streakLevel 2 → combo1, 3 → combo2, 4 → combo3, 5+ → combo4.
    /// </summary>
    public static void PlayCombo(int streakLevel)
    {
        if (streakLevel < 2) return;
        int idx = Math.Clamp(streakLevel - 2, 0, _combo.Length - 1);
        Play(_combo[idx]);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static byte[]? LoadBytes(string name)
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "Sfx", name + ".wav");
        if (!File.Exists(path)) return null;
        try   { return File.ReadAllBytes(path); }
        catch { return null; }
    }

    /// <summary>
    /// Creates a fresh SoundPlayer from the pre-loaded bytes and plays async.
    /// A new instance per call means concurrent sounds don't cancel each other.
    /// </summary>
    private static void Play(byte[]? data)
    {
        if (Muted || data == null) return;
        try
        {
            // Run on a thread-pool thread so we don't block the UI thread
            // while the SoundPlayer initialises from the stream.
            var bytes = data; // capture for lambda
            Task.Run(() =>
            {
                try
                {
                    using var ms     = new System.IO.MemoryStream(bytes);
                    using var player = new System.Media.SoundPlayer(ms);
                    player.PlaySync(); // sync inside the worker thread = fire-and-forget from UI perspective
                }
                catch { /* ignore audio errors */ }
            });
        }
        catch { /* ignore */ }
    }
}