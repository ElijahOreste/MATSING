namespace MATSING.Utils;

/// <summary>
/// Provides timer-based animation helpers for card controls.
/// All animations use <see cref="System.Windows.Forms.Timer"/> — no external libraries.
/// </summary>
public static class AnimationHelper
{
    // ── Colour constants ──────────────────────────────────────────────────
    public static readonly Color Gold   = Color.FromArgb(247, 201, 72);
    public static readonly Color Teal   = Color.FromArgb(94,  255, 209);
    public static readonly Color Red    = Color.FromArgb(255, 107, 107);

    // ─────────────────────────────────────────────────────────────────────
    //  Horizontal Squish Flip  (simulates a 3-D card flip with GDI+ width)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Animates a horizontal-squish flip: shrinks the control width to 0,
    /// calls <paramref name="onMidpoint"/>, then expands back to full width.
    /// </summary>
    /// <param name="control">The control to animate.</param>
    /// <param name="onMidpoint">Called at the halfway point to swap the visual.</param>
    /// <param name="onComplete">Called when the animation finishes.</param>
    public static void HorizontalFlip(Control control, Action onMidpoint, Action? onComplete = null)
    {
        int originalWidth = control.Width;
        int originalLeft  = control.Left;
        bool midpointDone = false;
        int step          = 0;
        const int TOTAL   = 12;   // animation frames

        var timer = new System.Windows.Forms.Timer { Interval = 18 };
        timer.Tick += (_, _) =>
        {
            step++;
            float t = (float)step / TOTAL;

            if (t <= 0.5f)
            {
                // Shrink phase
                float scale = 1f - (t / 0.5f);
                int newW = Math.Max(2, (int)(originalWidth * scale));
                int offset = (originalWidth - newW) / 2;
                control.SetBounds(originalLeft + offset, control.Top, newW, control.Height);
            }
            else
            {
                // Midpoint swap (only once)
                if (!midpointDone)
                {
                    midpointDone = true;
                    control.SetBounds(originalLeft, control.Top, 0, control.Height);
                    onMidpoint();
                }

                // Expand phase
                float scale = (t - 0.5f) / 0.5f;
                int newW = Math.Max(2, (int)(originalWidth * scale));
                int offset = (originalWidth - newW) / 2;
                control.SetBounds(originalLeft + offset, control.Top, newW, control.Height);
            }

            if (step >= TOTAL)
            {
                timer.Stop();
                timer.Dispose();
                control.SetBounds(originalLeft, control.Top, originalWidth, control.Height);
                control.Refresh();
                onComplete?.Invoke();
            }
        };
        timer.Start();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Gold Glow Pulse  (matched card shimmer)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Pulses a gold glow border on the control 3 times then stops.</summary>
    public static void GoldGlowPulse(Control control, Action? onComplete = null)
    {
        int pulses = 0;
        bool growing = true;
        int alpha = 0;

        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) =>
        {
            alpha += growing ? 25 : -25;
            if (alpha >= 255) { alpha = 255; growing = false; }
            if (alpha <= 0)   { alpha = 0;   growing = true; pulses++; }

            if (control is { } c)
            {
                c.Tag = Color.FromArgb(alpha, 247, 201, 72);   // store glow alpha in Tag
                c.Refresh();
            }

            if (pulses >= 3)
            {
                timer.Stop();
                timer.Dispose();
                if (control != null) { control.Tag = null; control.Refresh(); }
                onComplete?.Invoke();
            }
        };
        timer.Start();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Flash  (SpecialCard reveal: brief white flash)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Flashes the control with a translucent white overlay then fades.</summary>
    public static void Flash(Control control, Color flashColor, Action? onComplete = null)
    {
        int alpha = 200;
        var timer = new System.Windows.Forms.Timer { Interval = 25 };
        timer.Tick += (_, _) =>
        {
            alpha -= 20;
            if (control is { } c)
            {
                c.Tag = alpha > 0 ? (object?)Color.FromArgb(alpha, flashColor) : null;
                c.Refresh();
            }
            if (alpha <= 0)
            {
                timer.Stop();
                timer.Dispose();
                onComplete?.Invoke();
            }
        };
        timer.Start();
    }
}
