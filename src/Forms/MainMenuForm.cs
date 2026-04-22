using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// The main menu / splash screen for MATSING.
/// Players select difficulty and click PLAY to launch <see cref="GameForm"/>.
/// Uses full GDI+ custom painting for the deep-purple aesthetic.
/// </summary>
public class MainMenuForm : Form
{
    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg      = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2     = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold    = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed     = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal    = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColWhite   = Color.White;

    // ── State ─────────────────────────────────────────────────────────────
    private Difficulty _selectedDifficulty = Difficulty.Easy;
    private readonly Dictionary<Difficulty, int> _bestScores = new()
    {
        [Difficulty.Easy]   = 0,
        [Difficulty.Medium] = 0,
        [Difficulty.Hard]   = 0,
    };

    // ── Controls ──────────────────────────────────────────────────────────
    private Panel   _headerPanel  = null!;
    private Panel   _diffPanel    = null!;
    private Panel   _btnPanel     = null!;
    private Button  _playBtn      = null!;
    private Button  _easyBtn      = null!, _medBtn = null!, _hardBtn = null!;
    private Label   _bestLabel    = null!;

    // Logo animation
    private System.Windows.Forms.Timer _logoTimer = null!;
    private float _logoOffset = 0f;
    private bool  _logoUp     = true;

    // ── Constructor ───────────────────────────────────────────────────────
    public MainMenuForm()
    {
        InitialiseForm();
        BuildUI();
        StartLogoAnimation();
    }

    public void UpdateBestScore(Difficulty diff, int score)
    {
        if (score > _bestScores[diff]) _bestScores[diff] = score;
        RefreshBestLabel();
    }

    // ── Form Setup ────────────────────────────────────────────────────────
    private void InitialiseForm()
    {
        Text            = "MATSING – Matching Card Game";
        Size            = new Size(800, 620);
        MinimumSize     = new Size(700, 580);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Icon            = SystemIcons.Application;
    }

    // ── UI Construction ───────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── Header panel (custom paint for logo) ──────────────────────────
        _headerPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 220,
            BackColor = Color.Transparent,
        };
        _headerPanel.Paint += PaintHeader;

        // ── Difficulty panel ──────────────────────────────────────────────
        _diffPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 70,
            BackColor = Color.Transparent,
            Padding   = new Padding(20, 10, 20, 0),
        };

        _easyBtn = MakeDiffButton("🐒  Easy",   Difficulty.Easy);
        _medBtn  = MakeDiffButton("🧠  Medium", Difficulty.Medium);
        _hardBtn = MakeDiffButton("🔥  Hard",   Difficulty.Hard);

        var diffFlow = new FlowLayoutPanel
        {
            Dock            = DockStyle.Fill,
            FlowDirection   = FlowDirection.LeftToRight,
            WrapContents    = false,
            BackColor       = Color.Transparent,
            Padding         = new Padding(0),
        };
        diffFlow.Controls.AddRange(new Control[] { _easyBtn, _medBtn, _hardBtn });

        // Centre horizontally
        diffFlow.AutoSize         = true;
        diffFlow.AutoSizeMode     = AutoSizeMode.GrowAndShrink;
        var diffWrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        diffWrapper.Controls.Add(diffFlow);
        diffFlow.Location = new Point(
            (diffWrapper.Width - diffFlow.PreferredSize.Width) / 2, 5);
        diffWrapper.Resize += (_, _) =>
            diffFlow.Location = new Point(
                (diffWrapper.Width - diffFlow.PreferredSize.Width) / 2, 5);
        _diffPanel.Controls.Add(diffWrapper);

        // ── Best score label ──────────────────────────────────────────────
        _bestLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 36,
            ForeColor = ColTeal,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
        };
        RefreshBestLabel();

        // ── AOOP info panel ───────────────────────────────────────────────
        var aoopPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 100,
            BackColor = Color.FromArgb(30, 255, 255, 255),
            Margin    = new Padding(40),
        };
        aoopPanel.Paint += PaintAoopPanel;

        // ── Buttons panel ─────────────────────────────────────────────────
        _btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 110,
            BackColor = Color.Transparent,
        };

        _playBtn = MakePrimaryButton("▶   PLAY", ColGold, ColBg);
        _playBtn.Click += PlayBtn_Click;

        var quitBtn = MakePrimaryButton("✕   QUIT", ColBg2, ColWhite);
        quitBtn.Width = 140;
        quitBtn.Click += (_, _) => Application.Exit();

        var btnFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        btnFlow.Controls.AddRange(new Control[] { _playBtn, quitBtn });
        _btnPanel.Controls.Add(btnFlow);
        _btnPanel.Resize += (_, _) =>
            btnFlow.Location = new Point(
                (_btnPanel.Width  - btnFlow.PreferredSize.Width)  / 2,
                (_btnPanel.Height - btnFlow.PreferredSize.Height) / 2);

        // ── Add to form (bottom-up because Dock.Top stacks top-down) ──────
        Controls.Add(_btnPanel);
        Controls.Add(aoopPanel);
        Controls.Add(_bestLabel);
        Controls.Add(_diffPanel);
        Controls.Add(_headerPanel);

        HighlightDiffButton(_selectedDifficulty);
    }

    // ── Logo Animation ────────────────────────────────────────────────────
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

    // ── Custom Paint: Header ──────────────────────────────────────────────
    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Radial gradient background
        using var bgBrush = new SolidBrush(ColBg);
        g.FillRectangle(bgBrush, _headerPanel.ClientRectangle);

        float cy = _headerPanel.Height / 2f - 20 + _logoOffset;

        // Shadow of logo text
        using var shadowFont = new Font("Segoe UI Black", 52f, FontStyle.Bold, GraphicsUnit.Point);
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, ColRed));
        string logo = "🐾 MATSING 🐾";
        var    sz   = g.MeasureString(logo, shadowFont);
        float  lx   = (_headerPanel.Width - sz.Width) / 2f;
        float  ly   = cy - sz.Height / 2f;
        g.DrawString(logo, shadowFont, shadowBrush, lx + 5, ly + 5);

        // Main logo
        using var logoFont  = new Font("Segoe UI Black", 52f, FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(ColGold);
        g.DrawString(logo, logoFont, logoBrush, lx, ly);

        // Tagline
        using var tagFont  = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
        using var tagBrush = new SolidBrush(ColTeal);
        string tag     = "M A T C H I N G   C A R D   G A M E";
        var    tagSize = g.MeasureString(tag, tagFont);
        g.DrawString(tag, tagFont, tagBrush,
            (_headerPanel.Width - tagSize.Width) / 2f,
            ly + sz.Height + 4);
    }

    // ── Custom Paint: AOOP Info panel ─────────────────────────────────────
    private void PaintAoopPanel(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(Color.FromArgb(40, 59, 24, 120));
        g.FillRectangle(bgBrush, p.ClientRectangle);

        using var titleFont  = new Font("Segoe UI Black", 9f, FontStyle.Bold, GraphicsUnit.Point);
        using var titleBrush = new SolidBrush(ColGold);
        g.DrawString("AOOP PRINCIPLES DEMONSTRATED:", titleFont, titleBrush, 20, 10);

        string[] principles = {
            "① ABSTRACTION  →  CardBase (abstract), IFlippable, IScoreable",
            "② ENCAPSULATION  →  GameEngine (private state), ScoreTracker",
            "③ INHERITANCE  →  MonkeyCard : CardBase  |  CardControl : Panel",
            "④ POLYMORPHISM  →  PlayFlipAnimation() dispatched at runtime per type",
        };

        using var itemFont  = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var itemBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
        for (int i = 0; i < principles.Length; i++)
            g.DrawString(principles[i], itemFont, itemBrush, 20, 28 + i * 16);
    }

    // ── Button Factories ──────────────────────────────────────────────────
    private Button MakeDiffButton(string text, Difficulty diff)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(150, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = Color.Silver,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(6, 0, 6, 0),
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(59, 24, 120);
        btn.FlatAppearance.BorderSize  = 2;
        btn.Click += (_, _) =>
        {
            _selectedDifficulty = diff;
            HighlightDiffButton(diff);
            RefreshBestLabel();
        };
        return btn;
    }

    private Button MakePrimaryButton(string text, Color bg, Color fg)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(200, 54),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Font      = new Font("Segoe UI Black", 14f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(8, 0, 8, 0),
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = ControlPaint.Light(bg, 0.2f);
        btn.MouseLeave += (_, _) => btn.BackColor = bg;
        return btn;
    }

    private void HighlightDiffButton(Difficulty diff)
    {
        foreach (var (btn, d) in new[] {
            (_easyBtn, Difficulty.Easy),
            (_medBtn,  Difficulty.Medium),
            (_hardBtn, Difficulty.Hard) })
        {
            bool active = d == diff;
            btn.BackColor = active ? ColRed    : ColBg2;
            btn.ForeColor = active ? ColWhite  : Color.Silver;
            btn.FlatAppearance.BorderColor = active ? ColRed : Color.FromArgb(59, 24, 120);
        }
    }

    private void RefreshBestLabel()
    {
        int best = _bestScores[_selectedDifficulty];
        _bestLabel.Text = best > 0
            ? $"🏆  Best Score ({_selectedDifficulty}): {best} pts"
            : $"No record yet for {_selectedDifficulty} — be the first! 🐒";
    }

    // ── Play Button ───────────────────────────────────────────────────────
    private void PlayBtn_Click(object? sender, EventArgs e)
    {
        var gameForm = new GameForm(_selectedDifficulty);
        gameForm.GameFinished += (_, score) => UpdateBestScore(_selectedDifficulty, score);
        gameForm.ShowDialog(this);
    }

    // ── Dispose ───────────────────────────────────────────────────────────
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
