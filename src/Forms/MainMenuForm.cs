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
        // ─────────────────────────────────────────────
        // MAIN GRID (Header + Center)
        // ─────────────────────────────────────────────
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Center

        Controls.Add(mainLayout);

        // ─────────────────────────────────────────────
        // HEADER (custom painted logo)
        // ─────────────────────────────────────────────
        _headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        _headerPanel.Paint += PaintHeader;

        mainLayout.Controls.Add(_headerPanel, 0, 0);

        // ─────────────────────────────────────────────
        // CENTER (buttons area)
        // ─────────────────────────────────────────────
        var centerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Buttons
        _playBtn = MakePrimaryButton("▶   PLAY", ColGold, ColBg);
        _playBtn.Click += PlayBtn_Click;

        var quitBtn = MakePrimaryButton("✕   QUIT", ColBg2, ColWhite);
        quitBtn.Click += (_, _) => Application.Exit();
        
        _playBtn.Margin = new Padding(0, 0, 0, 30); // bottom gap
        quitBtn.Margin  = new Padding(0);
        // Button stack
        var btnFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };

        btnFlow.Controls.Add(_playBtn);
        btnFlow.Controls.Add(quitBtn);

        // 🔑 Center it inside the grid cell
        btnFlow.Anchor = AnchorStyles.None;

        centerLayout.Controls.Add(btnFlow, 0, 0);
        mainLayout.Controls.Add(centerLayout, 0, 1);

        // ─────────────────────────────────────────────
        // BEST SCORE LABEL (footer)
        // ─────────────────────────────────────────────
        _bestLabel = new Label
        {
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = ColTeal,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Padding = new Padding(0, 15, 0, 15),
            Height = 50
        };
        Controls.Add(_bestLabel);
        RefreshBestLabel();
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
            //HighlightDiffButton(diff);
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
