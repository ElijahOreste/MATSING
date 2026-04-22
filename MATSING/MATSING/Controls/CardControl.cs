using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Controls;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #3 — INHERITANCE
//  CardControl extends System.Windows.Forms.Panel, inheriting
//  all layout, docking, painting, and event infrastructure
//  from the WinForms Panel class.
//
//  AOOP PRINCIPLE #4 — POLYMORPHISM (base for virtual methods)
//  PlayFlipAnimation() and PlayMatchAnimation() are declared
//  virtual here so subclasses can override them with different
//  behaviours — enabling runtime polymorphism in GameForm.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>INHERITANCE</b> — CardControl extends <see cref="Panel"/><br/>
/// Base UI control for all card types. Inherits WinForms Panel behaviour
/// and adds card-specific painting, click handling, and virtual animation hooks.<br/><br/>
/// <b>POLYMORPHISM</b> — declares <c>virtual</c> animation methods that
/// subclasses override to produce different visual effects.
/// </summary>
public class CardControl : Panel
{
    // ── Constants ─────────────────────────────────────────────────────────
    protected static readonly Color ColBackground = Color.FromArgb(26,  10,  46);
    protected static readonly Color ColCardBack   = Color.FromArgb(59,  24, 120);
    protected static readonly Color ColCardFront  = Color.FromArgb(254, 249, 238);
    protected static readonly Color ColGold       = Color.FromArgb(247, 201,  72);
    protected static readonly Color ColTeal       = Color.FromArgb( 94, 255, 209);
    protected static readonly Color ColRed        = Color.FromArgb(255, 107, 107);
    protected static readonly Color ColText       = Color.White;
    private   static readonly int   CornerRadius  = 14;

    // ── Protected Card Data ───────────────────────────────────────────────
    /// <summary>The domain model card this control visualises.</summary>
    protected CardBase _cardData;

    /// <summary>Public read-only access to the underlying card model.</summary>
    public CardBase CardData => _cardData;

    /// <summary>Cached card image (loaded once, reused each paint).</summary>
    protected Image? _cardImage;

    /// <summary>True while an animation is in progress (blocks clicks).</summary>
    protected bool _isAnimating;

    // ── Public Event ──────────────────────────────────────────────────────
    /// <summary>Raised when the player clicks this card (and it is eligible to flip).</summary>
    public event EventHandler<CardBase>? CardClicked;

    // ── Constructor ───────────────────────────────────────────────────────
    /// <summary>
    /// Creates a CardControl bound to the given <paramref name="card"/>.
    /// Loads the card image and configures the panel for owner-drawing.
    /// </summary>
    public CardControl(CardBase card)
    {
        _cardData      = card;
        DoubleBuffered = true;
        Cursor         = Cursors.Hand;
        BorderStyle    = BorderStyle.None;

        // Load image — gracefully fall back if file missing
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, card.ImagePath);
        if (File.Exists(fullPath))
        {
            try { _cardImage = Image.FromFile(fullPath); }
            catch { _cardImage = null; }
        }

        Click    += OnCardClick;
        MouseEnter += (_, _) => { if (!_cardData.IsMatched) Invalidate(); };
        MouseLeave += (_, _) => Invalidate();
    }

    // ── Click Handler ─────────────────────────────────────────────────────
    /// <summary>Fires <see cref="CardClicked"/> if the card is eligible.</summary>
    protected virtual void OnCardClick(object? sender, EventArgs e)
    {
        if (_isAnimating)        return;
        if (_cardData.IsMatched) return;
        if (_cardData.IsFaceUp)  return;
        CardClicked?.Invoke(this, _cardData);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  VIRTUAL ANIMATION METHODS  ← POLYMORPHISM anchor points
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>POLYMORPHISM</b> — virtual method.<br/>
    /// Plays the card-flip animation. MonkeyCardControl and SpecialCardControl
    /// each OVERRIDE this to produce a different visual effect at runtime.
    /// </summary>
    public virtual void PlayFlipAnimation(Action? onComplete = null)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        AnimationHelper.HorizontalFlip(
            this,
            onMidpoint: Invalidate,   // repaint at midpoint with new face state
            onComplete: () =>
            {
                _isAnimating = false;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// <b>POLYMORPHISM</b> — virtual method.<br/>
    /// Plays the match-success animation. Overridden differently per subclass.
    /// </summary>
    public virtual void PlayMatchAnimation(Action? onComplete = null)
    {
        AnimationHelper.GoldGlowPulse(this, onComplete);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PAINTING  (GDI+)
    // ─────────────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        if (_cardData.IsFaceUp) DrawFront(g);
        else                    DrawBack(g);

        // Overlay glow alpha stored by AnimationHelper in Tag
        if (Tag is Color glowColor)
            DrawGlowOverlay(g, glowColor);

        // Hover highlight
        if (ClientRectangle.Contains(PointToClient(MousePosition))
            && !_cardData.IsFaceUp && !_cardData.IsMatched)
            DrawHoverHighlight(g);

        // Matched gold border
        if (_cardData.IsMatched)
            DrawMatchBorder(g);
    }

    // ── Protected Draw Helpers (overridable by subclasses) ────────────────

    /// <summary>Draws the card back face (deep purple + logo).</summary>
    protected virtual void DrawBack(Graphics g)
    {
        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        using var path = RoundedRect(rect, CornerRadius);

        // Fill
        using var bg = new SolidBrush(ColCardBack);
        g.FillPath(bg, path);

        // Border
        using var border = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f);
        g.DrawPath(border, path);

        // Watermark monkey emoji
        using var emojiFont = new Font("Segoe UI Emoji", Math.Max(8, Width / 4f), FontStyle.Regular, GraphicsUnit.Point);
        string emoji = "🐒";
        var emojiSize = g.MeasureString(emoji, emojiFont);
        using var dimBrush = new SolidBrush(Color.FromArgb(35, 255, 255, 255));
        g.DrawString(emoji, emojiFont, dimBrush,
            (Width - emojiSize.Width) / 2f,
            (Height - emojiSize.Height) / 2f - 10);

        // MATSING text
        using var logoFont = new Font("Segoe UI Black", Math.Max(5, Width / 10f), FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(Color.FromArgb(120, ColGold));
        string logo = "MATSING";
        var logoSize = g.MeasureString(logo, logoFont);
        g.DrawString(logo, logoFont, logoBrush,
            (Width - logoSize.Width) / 2f,
            Height - logoSize.Height - 10);
    }

    /// <summary>Draws the card front face (photo + label bar).</summary>
    protected virtual void DrawFront(Graphics g)
    {
        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        using var path = RoundedRect(rect, CornerRadius);
        g.SetClip(path);

        // Photo
        if (_cardImage != null)
            g.DrawImage(_cardImage, rect);
        else
        {
            using var fallback = new SolidBrush(Color.FromArgb(60, 30, 100));
            g.FillRectangle(fallback, rect);
        }

        // Gradient label bar at bottom
        int barH = Math.Max(24, Height / 5);
        var barRect = new Rectangle(1, Height - barH - 1, Width - 2, barH);
        using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Point(0, barRect.Top), new Point(0, barRect.Bottom),
            Color.FromArgb(0,   0, 0, 0),
            Color.FromArgb(200, 0, 0, 0));
        g.FillRectangle(grad, barRect);

        // Label text
        using var labelFont = new Font("Segoe UI Black", Math.Max(5f, Width / 14f), FontStyle.Bold, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(ColGold);
        var labelSize = g.MeasureString(_cardData.Label, labelFont);
        g.DrawString(_cardData.Label, labelFont, labelBrush,
            (Width - labelSize.Width) / 2f,
            Height - labelSize.Height - 6);

        g.ResetClip();

        // Border
        using var border = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f);
        using var borderPath = RoundedRect(rect, CornerRadius);
        g.DrawPath(border, borderPath);
    }

    private void DrawMatchBorder(Graphics g)
    {
        for (int i = 0; i < 3; i++)
        {
            var r = new Rectangle(i, i, Width - i * 2 - 1, Height - i * 2 - 1);
            using var p = new Pen(Color.FromArgb(200 - i * 50, ColGold), 2);
            using var path = RoundedRect(r, CornerRadius - i);
            g.DrawPath(p, path);
        }
    }

    private void DrawGlowOverlay(Graphics g, Color c)
    {
        using var b = new SolidBrush(c);
        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius);
        g.FillPath(b, path);
    }

    private void DrawHoverHighlight(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(120, ColTeal), 2);
        using var path = RoundedRect(new Rectangle(1, 1, Width - 2, Height - 2), CornerRadius);
        g.DrawPath(p, path);
    }

    // ── Utility: Rounded Rectangle Path ──────────────────────────────────
    protected static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X,             r.Y,              d, d, 180, 90);
        path.AddArc(r.Right - d,     r.Y,              d, d, 270, 90);
        path.AddArc(r.Right - d,     r.Bottom - d,     d, d,   0, 90);
        path.AddArc(r.X,             r.Bottom - d,     d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Dispose ───────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing) _cardImage?.Dispose();
        base.Dispose(disposing);
    }
}
