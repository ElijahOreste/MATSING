using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// The main landing page / menu for MATSING.
/// Players can start the game, view stats, or exit.
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
    private readonly Dictionary<Difficulty, int> _bestScores = new()
    {
        [Difficulty.Easy]   = 0,
        [Difficulty.Medium] = 0,
        [Difficulty.Hard]   = 0,
    };

    // ── Controls ──────────────────────────────────────────────────────────
    private Panel   _headerPanel  = null!;
    private Panel   _btnPanel     = null!;
    private Button  _startBtn     = null!;

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

        // ── Buttons panel (vertical layout) ────────────────────────────────
        _btnPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 50, 0, 50),
        };

        _startBtn = MakePrimaryButton("▶   START GAME", ColGold, ColBg);
        _startBtn.Click += StartBtn_Click;

        var statsBtn = MakePrimaryButton("📊   STATS", ColTeal, ColBg);
        statsBtn.Click += (_, _) => ShowStats();

        var quitBtn = MakePrimaryButton("✕   QUIT", ColBg2, ColWhite);
        quitBtn.Click += (_, _) => Application.Exit();

        var btnFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        btnFlow.Controls.AddRange(new Control[] { _startBtn, statsBtn, quitBtn });
        _btnPanel.Controls.Add(btnFlow);
        _btnPanel.Resize += (_, _) =>
            btnFlow.Location = new Point(
                (_btnPanel.Width  - btnFlow.PreferredSize.Width)  / 2,
                (_btnPanel.Height - btnFlow.PreferredSize.Height) / 2);

        // ── Add to form ───────────────────────────────────────────────────
        Controls.Add(_btnPanel);
        Controls.Add(_headerPanel);
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

    // ── Custom Paint: Info panel ──────────────────────────────────────────
    private void PaintInfoPanel(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(Color.FromArgb(40, 59, 24, 120));
        g.FillRectangle(bgBrush, p.ClientRectangle);

        using var titleFont  = new Font("Segoe UI Black", 10f, FontStyle.Bold, GraphicsUnit.Point);
        using var titleBrush = new SolidBrush(ColGold);
        g.DrawString("ABOUT MATSING:", titleFont, titleBrush, 20, 10);

        string[] principles = {
            "① ABSTRACTION  →  CardBase (abstract), IFlippable, IScoreable",
            "② ENCAPSULATION  →  GameEngine (private state), ScoreTracker",
            "③ INHERITANCE  →  MonkeyCard : CardBase  |  CardControl : Panel",
        };

        using var itemFont  = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        using var itemBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
        for (int i = 0; i < principles.Length; i++)
            g.DrawString(principles[i], itemFont, itemBrush, 20, 28 + i * 18);
    }

    // ── Button Factories ──────────────────────────────────────────────────
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
            Margin    = new Padding(0, 12, 0, 12),
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = ControlPaint.Light(bg, 0.2f);
        btn.MouseLeave += (_, _) => btn.BackColor = bg;
        return btn;
    }

    // ── Start Button ──────────────────────────────────────────────────────
    private void StartBtn_Click(object? sender, EventArgs e)
    {
        // Create difficulty selection form
        var diffForm = new DifficultySelectionForm(_bestScores);
        diffForm.FormClosed += (_, _) =>
        {
            // When difficulty form closes, check if game was completed
            if (diffForm.GameWasStarted)
            {
                UpdateBestScore(diffForm.SelectedDifficulty, diffForm.FinalScore);
            }
            diffForm.Dispose();
            // Reshow this form
            this.Show();
            this.BringToFront();
            this.Focus();
        };
        
        // Hide this form and show difficulty form
        this.Hide();
        diffForm.Show();
    }

    private void UpdateBestScore(Difficulty diff, int score)
    {
        if (score > _bestScores[diff]) _bestScores[diff] = score;
        _headerPanel.Invalidate();
    }

    private void ShowStats()
    {
        var statsForm = new StatsForm(_bestScores);
        statsForm.ShowDialog(this);
        statsForm.Dispose();
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
