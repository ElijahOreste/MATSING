using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// Statistics display form showing player's best scores and achievements.
/// </summary>
public class StatsForm : Form
{
    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg      = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2     = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold    = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed     = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal    = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColWhite   = Color.White;

    private readonly Dictionary<Difficulty, int> _bestScores;
    private Panel _headerPanel = null!;

    // Logo animation
    private System.Windows.Forms.Timer _logoTimer = null!;
    private float _logoOffset = 0f;
    private bool  _logoUp     = true;

    public StatsForm(Dictionary<Difficulty, int> bestScores)
    {
        _bestScores = bestScores ?? new();
        InitialiseForm();
        BuildUI();
        StartLogoAnimation();
    }

    private void InitialiseForm()
    {
        Text            = "MATSING – Your Statistics";
        Size            = new Size(700, 500);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Icon            = SystemIcons.Application;
    }

    private void BuildUI()
    {
        // ── Header panel ──────────────────────────────────────────────────
        _headerPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 140,
            BackColor = Color.Transparent,
        };
        _headerPanel.Paint += PaintHeader;

        // ── Stats content panel ───────────────────────────────────────────
        var contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(40, 30, 40, 30),
        };

        var statsFlow = new FlowLayoutPanel
        {
            FlowDirection   = FlowDirection.TopDown,
            AutoSize        = true,
            AutoSizeMode    = AutoSizeMode.GrowAndShrink,
            BackColor       = Color.Transparent,
        };

        // Title
        var titleLabel = new Label
        {
            Text      = "YOUR BEST SCORES",
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = ColGold,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 20),
        };
        statsFlow.Controls.Add(titleLabel);

        // Stats for each difficulty
        var stats = new (Difficulty diff, string emoji)[] {
            (Difficulty.Easy,   "🐒"),
            (Difficulty.Medium, "🧠"),
            (Difficulty.Hard,   "🔥"),
        };

        foreach (var (diff, emoji) in stats)
        {
            int best = _bestScores.TryGetValue(diff, out var score) ? score : 0;
            var label = new Label
            {
                Text      = $"{emoji} {diff} Difficulty:".PadRight(30) + (best > 0 ? $"{best:D5} pts" : "No record yet"),
                Font      = new Font("Segoe UI", 12f),
                ForeColor = ColTeal,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Margin    = new Padding(0, 10, 0, 10),
            };
            statsFlow.Controls.Add(label);
        }

        // Total stats
        int total = _bestScores.Values.Sum();
        statsFlow.Controls.Add(new Label { Height = 15, BackColor = Color.Transparent });
        var totalLabel = new Label
        {
            Text      = $"🏆 Total Score: {total:D6} pts",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColGold,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Margin    = new Padding(0, 10, 0, 0),
        };
        statsFlow.Controls.Add(totalLabel);

        contentPanel.Controls.Add(statsFlow);

        // ── Close button panel ────────────────────────────────────────────
        var btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 80,
            BackColor = Color.Transparent,
        };

        var closeBtn = new Button
        {
            Text      = "CLOSE",
            Size      = new Size(140, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = ColWhite,
            Font      = new Font("Segoe UI Black", 12f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Click += (_, _) => Close();
        closeBtn.MouseEnter += (_, _) => closeBtn.BackColor = ControlPaint.Light(ColBg2, 0.2f);
        closeBtn.MouseLeave += (_, _) => closeBtn.BackColor = ColBg2;

        btnPanel.Controls.Add(closeBtn);
        btnPanel.Resize += (_, _) =>
            closeBtn.Location = new Point(
                (btnPanel.Width - closeBtn.Width) / 2,
                (btnPanel.Height - closeBtn.Height) / 2);

        Controls.Add(btnPanel);
        Controls.Add(contentPanel);
        Controls.Add(_headerPanel);
    }

    private void StartLogoAnimation()
    {
        _logoTimer          = new System.Windows.Forms.Timer { Interval = 30 };
        _logoTimer.Tick    += (_, _) =>
        {
            _logoOffset += _logoUp ? 0.4f : -0.4f;
            if (_logoOffset >  8f) _logoUp = false;
            if (_logoOffset < -1f) _logoUp = true;
            _headerPanel.Invalidate();
        };
        _logoTimer.Start();
    }

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(ColBg);
        g.FillRectangle(bgBrush, _headerPanel.ClientRectangle);

        float cy = _headerPanel.Height / 2f - 20 + _logoOffset;

        using var shadowFont = new Font("Segoe UI Black", 38f, FontStyle.Bold, GraphicsUnit.Point);
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, ColRed));
        string logo = "YOUR ACHIEVEMENTS";
        var    sz   = g.MeasureString(logo, shadowFont);
        float  lx   = (_headerPanel.Width - sz.Width) / 2f;
        float  ly   = cy - sz.Height / 2f;
        g.DrawString(logo, shadowFont, shadowBrush, lx + 5, ly + 5);

        using var logoFont  = new Font("Segoe UI Black", 38f, FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(ColGold);
        g.DrawString(logo, logoFont, logoBrush, lx, ly);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoTimer?.Stop();
            _logoTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
