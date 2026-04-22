using MATSING.Models;

namespace MATSING.Game;

/// <summary>
/// Holds configuration values for a given difficulty level.
/// </summary>
public class DifficultyConfig
{
    public Difficulty Level      { get; }
    public int        PairCount  { get; }   // number of unique pairs
    public int        Columns    { get; }   // grid columns
    public int        TimeLimitS { get; }   // countdown in seconds
    public int        CardWidth  { get; }   // card pixel width
    public int        CardHeight { get; }   // card pixel height

    private DifficultyConfig(Difficulty level, int pairs, int cols, int time, int w, int h)
    {
        Level = level; PairCount = pairs; Columns = cols;
        TimeLimitS = time; CardWidth = w; CardHeight = h;
    }

    // ── Factory ───────────────────────────────────────────────────────────
    public static DifficultyConfig For(Difficulty d) => d switch
    {
        Difficulty.Easy   => new(d, 4, 4, 60,  140, 185),
        Difficulty.Medium => new(d, 6, 4, 90,  130, 170),
        Difficulty.Hard   => new(d, 9, 6, 120, 115, 150),
        _                 => throw new ArgumentOutOfRangeException(nameof(d))
    };
}
