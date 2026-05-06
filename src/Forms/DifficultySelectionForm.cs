using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// Difficulty and modifier selection screen where players choose their preferred difficulty level
/// and select optional game modifiers to increase challenge or change gameplay.
/// </summary>
public class DifficultySelectionForm : Form
{
    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg      = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2     = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold    = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed     = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal    = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColWhite   = Color.White;
    private static readonly Color ColGreen   = Color.FromArgb( 76, 175,  80);

    // ── State ─────────────────────────────────────────────────────────────
    private Difficulty _selectedDifficulty = Difficulty.Easy;
    private GameModifier _selectedModifiers = GameModifier.None;
    private readonly Dictionary<Difficulty, int> _bestScores;
    private bool _gameWasStarted = false;
    private int _finalScore = 0;

    // ── Controls ──────────────────────────────────────────────────────────
    private Panel   _headerPanel  = null!;
    private Panel   _contentPanel = null!;
    private Panel   _btnPanel     = null!;
    private Button  _playBtn      = null!;
    private Button  _easyBtn      = null!, _medBtn = null!, _hardBtn = null!;
    private Dictionary<GameModifier, Button> _modifierButtons = new();
    private GameForm? _gameForm = null;

    // Logo animation
    private System.Windows.Forms.Timer _logoTimer = null!;
    private float _logoOffset = 0f;
    private bool  _logoUp     = true;

    // ── Constructor ───────────────────────────────────────────────────────
    public DifficultySelectionForm(Dictionary<Difficulty, int> bestScores)
    {
        _bestScores = bestScores ?? new()
        {
            [Difficulty.Easy]   = 0,
            [Difficulty.Medium] = 0,
            [Difficulty.Hard]   = 0,
        };
        InitialiseForm();
        BuildUI();
        StartLogoAnimation();
    }

    // ── Form Setup ────────────────────────────────────────────────────────
    private void InitialiseForm()
    {
        Text            = "MATSING – Select Difficulty & Modifiers";
        Size            = new Size(900, 700);
        MinimumSize     = new Size(800, 650);
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
            Height    = 140,
            BackColor = Color.Transparent,
        };
        _headerPanel.Paint += PaintHeader;

        // ── Content panel (difficulty + modifiers) ────────────────────────
        _contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true,
            Padding   = new Padding(20),
        };

        // Difficulty section
        var diffLabel = new Label
        {
            Text      = "DIFFICULTY",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColGold,
            BackColor = Color.Transparent,
            AutoSize  = true,
        };

        var diffFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 10, 0, 20),
        };

        _easyBtn = MakeDiffButton("🐒  Easy",   Difficulty.Easy);
        _medBtn  = MakeDiffButton("🧠  Medium", Difficulty.Medium);
        _hardBtn = MakeDiffButton("🔥  Hard",   Difficulty.Hard);
        diffFlow.Controls.AddRange(new Control[] { _easyBtn, _medBtn, _hardBtn });

        // Modifiers section
        var modLabel = new Label
        {
            Text      = "MODIFIERS (Optional)",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColTeal,
            BackColor = Color.Transparent,
            AutoSize  = true,
        };

        var modFlow = new FlowLayoutPanel
        {
            FlowDirection   = FlowDirection.TopDown,
            AutoSize        = true,
            AutoSizeMode    = AutoSizeMode.GrowAndShrink,
            BackColor       = Color.Transparent,
            Padding         = new Padding(0, 10, 0, 20),
            WrapContents    = false,
        };

        var modifiers = new[] {
            GameModifier.CardDrift,
            GameModifier.ShrinkingCards,
            GameModifier.TripleMatch,
            GameModifier.FlipLimit,
            GameModifier.ZenMode,
            GameModifier.HardcoreMode,
            GameModifier.ComboMultiplier,
        };

        foreach (var mod in modifiers)
        {
            var btn = MakeModifierButton(mod);
            modFlow.Controls.Add(btn);
            _modifierButtons[mod] = btn;
        }

        // ── Buttons panel ─────────────────────────────────────────────────
        _btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 110,
            BackColor = Color.Transparent,
        };

        _playBtn = MakePrimaryButton("▶   START GAME", ColGold, ColBg);
        _playBtn.Click += PlayBtn_Click;

        var backBtn = MakePrimaryButton("◀   BACK", ColBg2, ColWhite);
        backBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var btnFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        btnFlow.Controls.AddRange(new Control[] { _playBtn, backBtn });
        _btnPanel.Controls.Add(btnFlow);
        _btnPanel.Resize += (_, _) =>
            btnFlow.Location = new Point(
                (_btnPanel.Width  - btnFlow.PreferredSize.Width)  / 2,
                (_btnPanel.Height - btnFlow.PreferredSize.Height) / 2);

        // Add controls to content panel
        var masterFlow = new FlowLayoutPanel
        {
            FlowDirection   = FlowDirection.TopDown,
            AutoSize        = true,
            AutoSizeMode    = AutoSizeMode.GrowAndShrink,
            BackColor       = Color.Transparent,
            Padding         = new Padding(0),
        };
        masterFlow.Controls.AddRange(new Control[] { diffLabel, diffFlow, modLabel, modFlow });
        _contentPanel.Controls.Add(masterFlow);

        // ── Add to form ───────────────────────────────────────────────────
        Controls.Add(_btnPanel);
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);

        HighlightDiffButton(_selectedDifficulty);
        HighlightModifiers();
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

        // Background
        using var bgBrush = new SolidBrush(ColBg);
        g.FillRectangle(bgBrush, _headerPanel.ClientRectangle);

        float cy = _headerPanel.Height / 2f - 20 + _logoOffset;

        // Shadow of logo text
        using var shadowFont = new Font("Segoe UI Black", 36f, FontStyle.Bold, GraphicsUnit.Point);
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, ColRed));
        string logo = "CHOOSE YOUR CHALLENGE";
        var    sz   = g.MeasureString(logo, shadowFont);
        float  lx   = (_headerPanel.Width - sz.Width) / 2f;
        float  ly   = cy - sz.Height / 2f;
        g.DrawString(logo, shadowFont, shadowBrush, lx + 5, ly + 5);

        // Main logo
        using var logoFont  = new Font("Segoe UI Black", 36f, FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(ColGold);
        g.DrawString(logo, logoFont, logoBrush, lx, ly);
    }

    // ── Button Factories ──────────────────────────────────────────────────
    private Button MakeDiffButton(string text, Difficulty diff)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(160, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = Color.Silver,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(8, 0, 8, 0),
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(59, 24, 120);
        btn.FlatAppearance.BorderSize  = 2;
        btn.Click += (_, _) =>
        {
            _selectedDifficulty = diff;
            HighlightDiffButton(diff);
        };
        return btn;
    }

    private Button MakeModifierButton(GameModifier mod)
    {
        string text = mod switch
        {
            GameModifier.CardDrift      => "🌀 Card Drift (cards swap every 20s)",
            GameModifier.ShrinkingCards => "📉 Shrinking Cards (cards shrink over time)",
            GameModifier.TripleMatch    => "🎲 Triple Match (match 3 instead of 2)",
            GameModifier.FlipLimit      => "🔒 Flip Limit (each card flips 2 times max)",
            GameModifier.ZenMode        => "🧘 Zen Mode (no timer, no penalties)",
            GameModifier.HardcoreMode   => "💀 Hardcore (one wrong flip ends game)",
            GameModifier.ComboMultiplier => "⚡ Combo Multiplier (build ×2, ×3, ×4 bonus)",
            _ => "Unknown"
        };

        var btn = new Button
        {
            Text      = text,
            Height    = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = Color.Silver,
            Font      = new Font("Segoe UI", 10f),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 4, 0, 4),
            Dock      = DockStyle.Top,
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(59, 24, 120);
        btn.FlatAppearance.BorderSize  = 2;
        btn.Click += (_, _) =>
        {
            // Toggle this modifier on/off
            if (_selectedModifiers.HasFlag(mod))
                _selectedModifiers &= ~mod;
            else
                _selectedModifiers |= mod;
            
            HighlightModifiers();
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

    private void HighlightModifiers()
    {
        foreach (var (mod, btn) in _modifierButtons)
        {
            bool isSelected = _selectedModifiers.HasFlag(mod);
            btn.BackColor = isSelected ? ColGreen  : ColBg2;
            btn.ForeColor = isSelected ? ColWhite  : Color.Silver;
            btn.FlatAppearance.BorderColor = isSelected ? ColGreen : Color.FromArgb(59, 24, 120);
        }
    }

    // ── Play Button ───────────────────────────────────────────────────────
    private void PlayBtn_Click(object? sender, EventArgs e)
    {
        // Dispose old game form if it still exists
        _gameForm?.Dispose();

        _gameForm = new GameForm(_selectedDifficulty);
        _gameForm.GameFinished += (_, score) =>
        {
            _finalScore = score;
            _gameWasStarted = true;
        };
        
        _gameForm.ShowDialog(this);
        _gameForm?.Dispose();
        _gameForm = null;

        // After game closes, close this form to return to main menu
        if (_gameWasStarted)
        {
            this.Close();
        }
    }

    // ── Public properties ─────────────────────────────────────────────────
    public Difficulty SelectedDifficulty => _selectedDifficulty;
    public GameModifier SelectedModifiers => _selectedModifiers;
    public bool GameWasStarted => _gameWasStarted;
    public int FinalScore => _finalScore;

    // ── Dispose ───────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logoTimer?.Stop();
            _logoTimer?.Dispose();
            _gameForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
