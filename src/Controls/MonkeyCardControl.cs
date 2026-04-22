using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Controls;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #4 — POLYMORPHISM  (primary showcase)
//  MonkeyCardControl OVERRIDES PlayFlipAnimation() and
//  PlayMatchAnimation() from CardControl.
//
//  When GameForm holds a List<CardControl> and calls:
//      control.PlayFlipAnimation();
//  …and the actual object is a MonkeyCardControl, THIS override
//  runs — not the base version. That is runtime polymorphism.
//
//  AOOP PRINCIPLE #3 — INHERITANCE
//  MonkeyCardControl extends CardControl which extends Panel.
//  Two levels of inheritance — all WinForms Panel infrastructure
//  is inherited by both.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>POLYMORPHISM + INHERITANCE</b> — MonkeyCardControl<br/>
/// Extends <see cref="CardControl"/> and overrides both animation methods
/// to produce monkey-specific visual effects (bounce flip + gold glow).<br/><br/>
/// GameForm calls <c>PlayFlipAnimation()</c> on a <c>CardControl</c> reference —
/// at <b>runtime</b> the CLR dispatches to THIS override automatically.
/// </summary>
public class MonkeyCardControl : CardControl
{
    private readonly MonkeyCard _monkey;

    /// <summary>Creates a MonkeyCardControl for the given <paramref name="card"/>.</summary>
    public MonkeyCardControl(MonkeyCard card) : base(card)
    {
        _monkey = card;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  POLYMORPHIC OVERRIDE #1 — Flip animation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>POLYMORPHISM</b> — overrides <see cref="CardControl.PlayFlipAnimation"/>.<br/>
    /// MonkeyCard flip: standard horizontal squish with a subtle bounce at the end.
    /// The base class version is NOT called here — completely different behaviour.
    /// </summary>
    public override void PlayFlipAnimation(Action? onComplete = null)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        AnimationHelper.HorizontalFlip(
            this,
            onMidpoint: Invalidate,
            onComplete: () =>
            {
                _isAnimating = false;
                PlayBounce(onComplete);   // extra bounce unique to MonkeyCard
            });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  POLYMORPHIC OVERRIDE #2 — Match animation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>POLYMORPHISM</b> — overrides <see cref="CardControl.PlayMatchAnimation"/>.<br/>
    /// MonkeyCard match: gold glow pulse (warm, celebratory feel).
    /// </summary>
    public override void PlayMatchAnimation(Action? onComplete = null)
    {
        AnimationHelper.GoldGlowPulse(this, onComplete);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  OVERRIDE — DrawFront adds character name badge
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws the card front and adds the monkey's <see cref="MonkeyCard.CharacterName"/>
    /// as a small badge — a MonkeyCard-specific visual detail.
    /// </summary>
    protected override void DrawFront(System.Drawing.Graphics g)
    {
        base.DrawFront(g);   // inherit the standard front draw

        // Character name badge (top-left corner)
        using var badgeFont = new Font("Segoe UI", Math.Max(4f, Width / 16f),
                                       FontStyle.Bold, GraphicsUnit.Point);
        using var badgeBg   = new SolidBrush(Color.FromArgb(160, 26, 10, 46));
        using var badgeFg   = new SolidBrush(Color.FromArgb(200, 94, 255, 209));

        string name     = _monkey.CharacterName;
        var    nameSize = g.MeasureString(name, badgeFont);
        var    badgeR   = new RectangleF(6, 6, nameSize.Width + 8, nameSize.Height + 4);

        g.FillRectangle(badgeBg, badgeR);
        g.DrawString(name, badgeFont, badgeFg, badgeR.X + 4, badgeR.Y + 2);
    }

    // ── Private: tiny bounce effect ───────────────────────────────────────
    private void PlayBounce(Action? onComplete)
    {
        int originalTop = Top;
        int step = 0;
        float[] offsets = { -5f, -9f, -5f, 0f, -2f, 0f };

        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) =>
        {
            if (step < offsets.Length)
            {
                Top = originalTop + (int)offsets[step++];
            }
            else
            {
                Top = originalTop;
                timer.Stop();
                timer.Dispose();
                onComplete?.Invoke();
            }
        };
        timer.Start();
    }
}
