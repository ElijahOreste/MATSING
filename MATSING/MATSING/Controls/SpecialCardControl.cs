using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Controls;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #4 — POLYMORPHISM  (second override)
//  SpecialCardControl overrides the SAME virtual methods as
//  MonkeyCardControl, but produces COMPLETELY DIFFERENT effects.
//
//  In GameForm:
//      List<CardControl> _controls = ...
//      foreach (var c in _controls)
//          c.PlayMatchAnimation();   // ← ONE call
//
//  If c is MonkeyCardControl  → gold glow pulse
//  If c is SpecialCardControl → white flash + teal shimmer
//  The caller never writes an if/switch — CLR dispatches at runtime.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>POLYMORPHISM</b> — SpecialCardControl<br/>
/// Overrides both animation methods from <see cref="CardControl"/>
/// with effects distinct from <see cref="MonkeyCardControl"/>.<br/>
/// Demonstrates that the same virtual method call on a
/// <c>CardControl</c> reference can behave in entirely different ways
/// depending on the concrete type at runtime.
/// </summary>
public class SpecialCardControl : CardControl
{
    private readonly SpecialCard _special;

    /// <summary>Creates a SpecialCardControl.</summary>
    public SpecialCardControl(SpecialCard card) : base(card)
    {
        _special = card;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  POLYMORPHIC OVERRIDE #1 — Flip animation (DIFFERENT from MonkeyCard)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>POLYMORPHISM</b> — overrides <see cref="CardControl.PlayFlipAnimation"/>.<br/>
    /// SpecialCard flip: fast horizontal flip followed by a white flash overlay —
    /// completely different from the MonkeyCard bounce.
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
                // Flash effect unique to SpecialCard
                AnimationHelper.Flash(this, Color.White, onComplete: () =>
                {
                    _isAnimating = false;
                    onComplete?.Invoke();
                });
            });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  POLYMORPHIC OVERRIDE #2 — Match animation (DIFFERENT from MonkeyCard)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>POLYMORPHISM</b> — overrides <see cref="CardControl.PlayMatchAnimation"/>.<br/>
    /// SpecialCard match: teal shimmer (vs MonkeyCard's gold glow).
    /// </summary>
    public override void PlayMatchAnimation(Action? onComplete = null)
    {
        AnimationHelper.Flash(this, Color.FromArgb(94, 255, 209), onComplete);
    }

    // ── Override DrawFront — adds "⭐ SPECIAL" badge ─────────────────────

    /// <summary>
    /// Draws card front with an extra gold "⭐ SPECIAL" badge at top-right.
    /// </summary>
    protected override void DrawFront(System.Drawing.Graphics g)
    {
        base.DrawFront(g);

        using var badgeFont = new Font("Segoe UI Black", Math.Max(5f, Width / 14f),
                                       FontStyle.Bold, GraphicsUnit.Point);
        using var badgeBg   = new SolidBrush(Color.FromArgb(200, 247, 201, 72));
        using var badgeFg   = new SolidBrush(Color.FromArgb(26, 10, 46));

        string badge   = "⭐ SPECIAL";
        var    bSize   = g.MeasureString(badge, badgeFont);
        var    bRect   = new RectangleF(Width - bSize.Width - 10, 6,
                                        bSize.Width + 6, bSize.Height + 4);
        g.FillRectangle(badgeBg, bRect);
        g.DrawString(badge, badgeFont, badgeFg, bRect.X + 3, bRect.Y + 2);
    }
}
