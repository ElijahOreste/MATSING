namespace MATSING.Game;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #2 — ENCAPSULATION
//  GameTimer wraps System.Windows.Forms.Timer and hides all
//  tick/interval wiring. External code only sees Start, Stop,
//  Reset, and the two public events.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ENCAPSULATION</b> — GameTimer<br/>
/// Wraps a <see cref="System.Windows.Forms.Timer"/> to provide a clean,
/// game-oriented countdown API. The raw timer object, interval, and
/// tick handler are private implementation details.
/// </summary>
public sealed class GameTimer : IDisposable
{
    // ── Private Implementation ────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _inner;
    private int _totalSeconds;
    private int _elapsed;
    private bool _running;

    // ── Public Events ─────────────────────────────────────────────────────
    /// <summary>Raised every second with the current elapsed and remaining seconds.</summary>
    public event EventHandler<TimerTickEventArgs>? Tick;

    /// <summary>Raised when the countdown reaches zero.</summary>
    public event EventHandler? TimeUp;

    // ── Public Read-Only Properties ───────────────────────────────────────
    /// <summary>Seconds elapsed since last <see cref="Start"/>.</summary>
    public int Elapsed => _elapsed;

    /// <summary>Seconds remaining before <see cref="TimeUp"/> fires.</summary>
    public int Remaining => Math.Max(0, _totalSeconds - _elapsed);

    /// <summary>Fraction 0.0–1.0 of time remaining.</summary>
    public float RemainingFraction =>
        _totalSeconds == 0 ? 0f : (float)Remaining / _totalSeconds;

    // ── Constructor ───────────────────────────────────────────────────────
    public GameTimer()
    {
        _inner          = new System.Windows.Forms.Timer();
        _inner.Interval = 1000;
        _inner.Tick    += InternalTick;
    }

    // ── Public API ────────────────────────────────────────────────────────
    /// <summary>Starts a new countdown from <paramref name="totalSeconds"/>.</summary>
    public void Start(int totalSeconds)
    {
        _totalSeconds = totalSeconds;
        _elapsed      = 0;
        _running      = true;
        _inner.Start();
    }

    /// <summary>Pauses the timer without resetting elapsed time.</summary>
    public void Stop()
    {
        _running = false;
        _inner.Stop();
    }

    /// <summary>Stops and resets elapsed time to zero.</summary>
    public void Reset()
    {
        Stop();
        _elapsed = 0;
    }

    // ── Private Tick Handler ──────────────────────────────────────────────
    private void InternalTick(object? sender, EventArgs e)
    {
        if (!_running) return;
        _elapsed++;
        Tick?.Invoke(this, new TimerTickEventArgs(_elapsed, Remaining, RemainingFraction));
        if (_elapsed >= _totalSeconds)
        {
            Stop();
            TimeUp?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _inner.Stop();
        _inner.Dispose();
    }
}

/// <summary>Event data for <see cref="GameTimer.Tick"/>.</summary>
public class TimerTickEventArgs : EventArgs
{
    public int   Elapsed          { get; }
    public int   Remaining        { get; }
    public float RemainingFraction{ get; }

    public TimerTickEventArgs(int elapsed, int remaining, float fraction)
    {
        Elapsed           = elapsed;
        Remaining         = remaining;
        RemainingFraction = fraction;
    }
}
