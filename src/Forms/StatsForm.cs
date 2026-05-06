using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Forms;

/// <summary>
/// Statistics display form showing player's best scores and achievements.
/// Fully custom-painted for a rich, centred layout.
/// </summary>
public class StatsForm : Form
{
    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg      = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2     = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColCard    = Color.FromArgb( 55,  20, 100);
    private static readonly Color ColGold    = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed     = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal    = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColPurple  = Color.FromArgb(167, 139, 250);
    private static readonly Color ColWhite   = Color.White;

    private readonly Dictionary<Difficulty, int> _bestScores;
    private Panel  _headerPanel  = null!;
    private Panel  _canvasPanel  = null!;

    // Logo animation
    private System.Windows.Forms.Timer _logoTimer = null!;
    private float _logoOffset = 0f;
    private bool  _logoUp     = true;

    // Shimmer animation on the stat cards
    private System.Windows.Forms.Timer _shimmerTimer = null!;
    private float _shimmerX = -1f; // 0..1 progress across card width

    public StatsForm(Dictionary<Difficulty, int> bestScores)
    {
        _bestScores = bestScores ?? new();
        InitialiseForm();
        BuildUI();
        StartAnimations();
    }

    // ── Form Setup ────────────────────────────────────────────────────────
    private void InitialiseForm()
    {
        Text            = "MATSING – Your Statistics";
        Size            = new Size(720, 560);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Icon            = SystemIcons.Application;
    }

    // ── UI Construction ───────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── Animated header ───────────────────────────────────────────────
        _headerPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 120,
            BackColor = Color.Transparent,
        };
        _headerPanel.Paint += PaintHeader;

        // ── Main canvas — all stat cards drawn via GDI+ ──────────────────
        _canvasPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
        };
        _canvasPanel.Paint += PaintCanvas;

        // ── Close button ──────────────────────────────────────────────────
        var btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 76,
            BackColor = Color.Transparent,
        };

        var closeBtn = new Button
        {
            Text      = "◀  CLOSE",
            Size      = new Size(160, 48),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = ColWhite,
            Font      = new Font("Segoe UI Black", 12f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
        };
        closeBtn.FlatAppearance.BorderSize  = 2;
        closeBtn.FlatAppearance.BorderColor = ColPurple;
        closeBtn.Click     += (_, _) => { SfxPlayer.PlayClick(); Close(); };
        closeBtn.MouseEnter += (_, _) => { closeBtn.BackColor = ColPurple; closeBtn.ForeColor = ColBg; };
        closeBtn.MouseLeave += (_, _) => { closeBtn.BackColor = ColBg2;   closeBtn.ForeColor = ColWhite; };

        btnPanel.Controls.Add(closeBtn);
        btnPanel.Resize += (_, _) =>
            closeBtn.Location = new Point(
                (btnPanel.Width  - closeBtn.Width)  / 2,
                (btnPanel.Height - closeBtn.Height) / 2);

        Controls.Add(btnPanel);
        Controls.Add(_canvasPanel);
        Controls.Add(_headerPanel);

        // Enable double-buffering on canvasPanel via reflection
        typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_canvasPanel, true);
    }

    // ── Animations ────────────────────────────────────────────────────────
    private void StartAnimations()
    {
        _logoTimer       = new System.Windows.Forms.Timer { Interval = 30 };
        _logoTimer.Tick += (_, _) =>
        {
            _logoOffset += _logoUp ? 0.4f : -0.4f;
            if (_logoOffset >  8f) _logoUp = false;
            if (_logoOffset < -1f) _logoUp = true;
            _headerPanel.Invalidate();
        };
        _logoTimer.Start();

        _shimmerTimer       = new System.Windows.Forms.Timer { Interval = 16 };
        _shimmerTimer.Tick += (_, _) =>
        {
            _shimmerX += 0.012f;
            if (_shimmerX > 1.4f) _shimmerX = -0.4f;
            _canvasPanel.Invalidate();
        };
        _shimmerTimer.Start();
    }

    // ── Paint: Header ─────────────────────────────────────────────────────
    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(ColBg);
        g.FillRectangle(bgBrush, _headerPanel.ClientRectangle);

        float cy = _headerPanel.Height / 2f - 10 + _logoOffset;
        using var shadowFont  = new Font("Segoe UI Black", 32f, FontStyle.Bold, GraphicsUnit.Point);
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, ColRed));
        string title = "🏆  YOUR ACHIEVEMENTS  🏆";
        var    sz    = g.MeasureString(title, shadowFont);
        float  lx    = (_headerPanel.Width - sz.Width) / 2f;
        float  ly    = cy - sz.Height / 2f;
        g.DrawString(title, shadowFont, shadowBrush, lx + 4, ly + 4);
        using var logoFont  = new Font("Segoe UI Black", 32f, FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(ColGold);
        g.DrawString(title, logoFont, logoBrush, lx, ly);

        // Decorative separator line
        using var linePen = new Pen(Color.FromArgb(60, ColTeal), 1.5f);
        float lineY = ly + sz.Height + 6;
        g.DrawLine(linePen, lx, lineY, lx + sz.Width, lineY);
    }

    // ── Paint: Stat Cards ─────────────────────────────────────────────────
    private void PaintCanvas(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode    = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint= System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int w = _canvasPanel.Width;
        int h = _canvasPanel.Height;

        // ── Three difficulty stat cards ───────────────────────────────────
        var cards = new[]
        {
            (Difficulty.Easy,   "🐒", "Easy",   ColTeal,  "Beginner run"),
            (Difficulty.Medium, "🧠", "Medium", ColGold,  "Intermediate"),
            (Difficulty.Hard,   "🔥", "Hard",   ColRed,   "Expert run"),
        };

        int cardW   = 170;
        int cardH   = 160;
        int gap     = 20;
        int totalW  = cards.Length * cardW + (cards.Length - 1) * gap;
        int startX  = (w - totalW) / 2;
        int cardY   = (h - cardH) / 2 - 30;

        for (int i = 0; i < cards.Length; i++)
        {
            var (diff, emoji, label, accent, subtitle) = cards[i];
            int best = _bestScores.TryGetValue(diff, out var sc) ? sc : 0;
            int cx   = startX + i * (cardW + gap);

            DrawStatCard(g, cx, cardY, cardW, cardH,
                emoji, label, subtitle, best, accent);
        }

        // ── Total score bar ───────────────────────────────────────────────
        int total  = _bestScores.Values.Sum();
        int barW   = totalW;
        int barX   = startX;
        int barY   = cardY + cardH + 22;
        DrawTotalBar(g, barX, barY, barW, total);
    }

    // ── Draw a single stat card ───────────────────────────────────────────
    private void DrawStatCard(Graphics g, int x, int y, int w, int h,
        string emoji, string label, string subtitle, int best, Color accent)
    {
        var rect = new Rectangle(x, y, w, h);

        // Card background with rounded rect
        using var bgBrush = new SolidBrush(ColCard);
        FillRoundRect(g, bgBrush, rect, 14);

        // Accent top strip
        using var accentBrush = new SolidBrush(accent);
        var stripRect = new Rectangle(x, y, w, 6);
        FillRoundRect(g, accentBrush, stripRect, 6);

        // Shimmer sweep
        using var shimmerBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new PointF(x + _shimmerX * w - 30, y),
            new PointF(x + _shimmerX * w + 30, y + 1),
            Color.FromArgb(0,  Color.White),
            Color.FromArgb(18, Color.White));
        g.FillRectangle(shimmerBrush, x, y, w, h);

        // Card border
        using var borderPen = new Pen(Color.FromArgb(60, accent), 1.5f);
        DrawRoundRect(g, borderPen, rect, 14);

        // Emoji
        using var emojiFont = new Font("Segoe UI Emoji", 22f, GraphicsUnit.Point);
        var emojiSz = g.MeasureString(emoji, emojiFont);
        g.DrawString(emoji, emojiFont, Brushes.White,
            x + (w - emojiSz.Width) / 2f, y + 14);

        // Difficulty label
        using var labelFont  = new Font("Segoe UI Black", 11f, FontStyle.Bold, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(ColWhite);
        var labelSz = g.MeasureString(label, labelFont);
        g.DrawString(label, labelFont, labelBrush,
            x + (w - labelSz.Width) / 2f, y + 58);

        // Subtitle
        using var subFont  = new Font("Segoe UI", 8f, GraphicsUnit.Point);
        using var subBrush = new SolidBrush(Color.FromArgb(140, ColWhite));
        var subSz = g.MeasureString(subtitle, subFont);
        g.DrawString(subtitle, subFont, subBrush,
            x + (w - subSz.Width) / 2f, y + 76);

        // Score value
        string scoreStr = best > 0 ? $"{best:N0}" : "—";
        using var scoreFont  = new Font("Segoe UI Black", 18f, FontStyle.Bold, GraphicsUnit.Point);
        using var scoreBrush = new SolidBrush(best > 0 ? accent : Color.FromArgb(80, ColWhite));
        var scoreSz = g.MeasureString(scoreStr, scoreFont);
        g.DrawString(scoreStr, scoreFont, scoreBrush,
            x + (w - scoreSz.Width) / 2f, y + 96);

        // "pts" label
        if (best > 0)
        {
            using var ptsFont  = new Font("Segoe UI", 8f, GraphicsUnit.Point);
            using var ptsBrush = new SolidBrush(Color.FromArgb(120, accent));
            var ptsSz = g.MeasureString("pts", ptsFont);
            g.DrawString("pts", ptsFont, ptsBrush,
                x + (w - ptsSz.Width) / 2f, y + 120);
        }
        else
        {
            using var noFont  = new Font("Segoe UI", 8f, FontStyle.Italic, GraphicsUnit.Point);
            using var noBrush = new SolidBrush(Color.FromArgb(80, ColWhite));
            string noTxt = "no record yet";
            var noSz = g.MeasureString(noTxt, noFont);
            g.DrawString(noTxt, noFont, noBrush,
                x + (w - noSz.Width) / 2f, y + 120);
        }
    }

    // ── Draw total score bar ──────────────────────────────────────────────
    private void DrawTotalBar(Graphics g, int x, int y, int w, int total)
    {
        int bh = 52;
        var rect = new Rectangle(x, y, w, bh);

        using var bgBrush = new SolidBrush(ColCard);
        FillRoundRect(g, bgBrush, rect, 10);

        using var borderPen = new Pen(Color.FromArgb(60, ColGold), 1.5f);
        DrawRoundRect(g, borderPen, rect, 10);

        // Trophy icon + label
        using var labelFont  = new Font("Segoe UI", 11f, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(Color.FromArgb(160, ColWhite));
        g.DrawString("COMBINED BEST", labelFont, labelBrush, x + 20, y + 8);

        // Score
        string scoreStr = total > 0 ? $"🏆  {total:N0} pts" : "🏆  No scores yet";
        using var scoreFont  = new Font("Segoe UI Black", 14f, FontStyle.Bold, GraphicsUnit.Point);
        using var scoreBrush = new SolidBrush(ColGold);
        var scoreSz = g.MeasureString(scoreStr, scoreFont);
        g.DrawString(scoreStr, scoreFont, scoreBrush,
            x + w - scoreSz.Width - 20, y + (bh - scoreSz.Height) / 2f);
    }

    // ── GDI+ Helpers ──────────────────────────────────────────────────────
    private static void FillRoundRect(Graphics g, Brush brush, Rectangle r, int radius)
    {
        using var path = RoundRectPath(r, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundRect(Graphics g, Pen pen, Rectangle r, int radius)
    {
        using var path = RoundRectPath(r, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundRectPath(Rectangle r, int rad)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = rad * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Dispose ───────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoTimer?.Stop();
            _logoTimer?.Dispose();
            _shimmerTimer?.Stop();
            _shimmerTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}