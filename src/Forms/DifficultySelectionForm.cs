using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Forms;

public class DifficultySelectionForm : Form
{
    private static readonly Color ColBg    = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2   = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold  = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed   = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal  = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColWhite = Color.White;
    private static readonly Color ColGreen = Color.FromArgb( 76, 175,  80);

    private Difficulty   _selectedDifficulty = Difficulty.Easy;
    private GameModifier _selectedModifiers  = GameModifier.None;
    private readonly Dictionary<Difficulty, int> _bestScores;
    private bool _gameWasStarted = false;
    private int  _finalScore     = 0;

    private Panel  _headerPanel  = null!;
    private Panel  _contentPanel = null!;
    private Panel  _btnPanel     = null!;
    private Button _playBtn      = null!;
    private Button _easyBtn = null!, _medBtn = null!, _hardBtn = null!;
    private Dictionary<GameModifier, Button> _modifierButtons = new();

    private System.Windows.Forms.Timer _logoTimer = null!;
    private float _logoOffset = 0f;
    private bool  _logoUp     = true;

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

    private void InitialiseForm()
    {
        Text            = "MATSING – Select Difficulty & Modifiers";
        Size            = new Size(900, 720);
        MinimumSize     = new Size(800, 650);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Icon            = SystemIcons.Application;
    }

    private void BuildUI()
    {
        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.Transparent };
        _headerPanel.Paint += PaintHeader;

        _btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.Transparent };
        _playBtn  = MakePrimaryButton("▶   START GAME", ColGold, ColBg);
        _playBtn.Click += PlayBtn_Click;

        var backBtn = MakePrimaryButton("◀   BACK", ColBg2, ColWhite);
        backBtn.Click += (_, _) => { SfxPlayer.PlayClick(); Close(); };

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

        _contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };

        var inner = new Panel
        {
            BackColor    = Color.Transparent,
            Width        = 760,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };

        var diffLabel = new Label
        {
            Text      = "DIFFICULTY",
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColGold,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(0, 8),
        };

        _easyBtn = MakeDiffButton("🐒  Easy",   Difficulty.Easy);
        _medBtn  = MakeDiffButton("🧠  Medium", Difficulty.Medium);
        _hardBtn = MakeDiffButton("🔥  Hard",   Difficulty.Hard);

        int diffBtnW = 220, diffBtnH = 52, diffGap = 16;
        int diffRowW = 3 * diffBtnW + 2 * diffGap;
        int diffOffX = (760 - diffRowW) / 2;
        int diffY    = 36;
        _easyBtn.SetBounds(diffOffX,                          diffY, diffBtnW, diffBtnH);
        _medBtn .SetBounds(diffOffX + diffBtnW + diffGap,     diffY, diffBtnW, diffBtnH);
        _hardBtn.SetBounds(diffOffX + 2*(diffBtnW + diffGap), diffY, diffBtnW, diffBtnH);

        var modLabel = new Label
        {
            Text      = "MODIFIERS  (toggle on/off)",
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ColTeal,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(0, diffY + diffBtnH + 18),
        };

        // Updated modifier list with new names
        var modifiers = new[]
        {
            GameModifier.CardDrift,
            GameModifier.ShrinkingCards,
            GameModifier.MemoryLeakMode,
            GameModifier.GhostCard,
            GameModifier.ZenMode,
            GameModifier.HardcoreMode,
            GameModifier.ComboMultiplier,
            GameModifier.EndlessMode,
        };

        int modBtnW = 370, modBtnH = 46, modGapX = 16, modGapY = 8;
        int modStartY = modLabel.Location.Y + 28;
        int col0X = (760 - (2 * modBtnW + modGapX)) / 2;
        int col1X = col0X + modBtnW + modGapX;

        for (int i = 0; i < modifiers.Length; i++)
        {
            var mod = modifiers[i];
            int col = i % 2;
            int row = i / 2;
            int bx  = col == 0 ? col0X : col1X;
            int by  = modStartY + row * (modBtnH + modGapY);

            var btn = MakeModifierButton(mod);
            btn.SetBounds(bx, by, modBtnW, modBtnH);
            inner.Controls.Add(btn);
            _modifierButtons[mod] = btn;
        }

        // If odd number, last button spans full width
        if (modifiers.Length % 2 == 1)
        {
            var lastBtn = _modifierButtons[modifiers[^1]];
            lastBtn.SetBounds(col0X, lastBtn.Top, 2 * modBtnW + modGapX, modBtnH);
        }

        int lastModRow = (modifiers.Length - 1) / 2;
        int innerH     = modStartY + (lastModRow + 1) * (modBtnH + modGapY) + 10;
        inner.Height   = innerH;

        inner.Controls.AddRange(new Control[] { diffLabel, _easyBtn, _medBtn, _hardBtn, modLabel });
        _contentPanel.Controls.Add(inner);
        _contentPanel.Resize += (_, _) =>
        {
            inner.Left = Math.Max(0, (_contentPanel.Width - inner.Width) / 2);
            inner.Top  = Math.Max(8, (_contentPanel.Height - inner.Height) / 2);
        };

        Controls.Add(_btnPanel);
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);

        HighlightDiffButton(_selectedDifficulty);
        HighlightModifiers();
    }

    private void StartLogoAnimation()
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
    }

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bgBrush = new SolidBrush(ColBg);
        g.FillRectangle(bgBrush, _headerPanel.ClientRectangle);

        float cy = _headerPanel.Height / 2f - 20 + _logoOffset;
        using var sf  = new Font("Segoe UI Black", 36f, FontStyle.Bold, GraphicsUnit.Point);
        using var shb = new SolidBrush(Color.FromArgb(80, ColRed));
        string logo = "CHOOSE YOUR CHALLENGE";
        var    sz   = g.MeasureString(logo, sf);
        float  lx   = (_headerPanel.Width - sz.Width) / 2f;
        float  ly   = cy - sz.Height / 2f;
        g.DrawString(logo, sf, shb, lx + 5, ly + 5);
        using var lgb = new SolidBrush(ColGold);
        g.DrawString(logo, sf, lgb, lx, ly);
    }

    private Button MakeDiffButton(string text, Difficulty diff)
    {
        var btn = new Button
        {
            Text      = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = Color.Silver,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(59, 24, 120);
        btn.FlatAppearance.BorderSize  = 2;
        btn.Click += (_, _) =>
        {
            SfxPlayer.PlayClick();
            _selectedDifficulty = diff;
            HighlightDiffButton(diff);
        };
        return btn;
    }

    private Button MakeModifierButton(GameModifier mod)
    {
        string text = mod switch
        {
            GameModifier.CardDrift       => "🌀  Card Drift",
            GameModifier.ShrinkingCards  => "📉  Shrinking Cards",
            GameModifier.MemoryLeakMode  => "👻  Memory Leak",
            GameModifier.GhostCard       => "🫥  Ghost Card",
            GameModifier.ZenMode         => "🧘  Zen Mode",
            GameModifier.HardcoreMode    => "❤️  Hardcore",
            GameModifier.ComboMultiplier => "⚡  Combo Multiplier",
            GameModifier.EndlessMode     => "♾️  Endless Mode",
            _                            => "Unknown"
        };

        string tooltip = mod switch
        {
            GameModifier.CardDrift       => "Cards swap positions every 20s",
            GameModifier.ShrinkingCards  => "Cards shrink smaller over time",
            GameModifier.MemoryLeakMode  => "Decoy cards injected — flip once then vanish, wasting your turn",
            GameModifier.GhostCard       => "40% chance a flipped card shows blank first for 0.6s",
            GameModifier.ZenMode         => "No timer — pure relaxation",
            GameModifier.HardcoreMode    => "3/4/5 hearts by difficulty — lose one per mismatch",
            GameModifier.ComboMultiplier => "Build ×2, ×3, ×4 score bonus on consecutive matches",
            GameModifier.EndlessMode     => "Board resets on clear — play until time runs out",
            _                            => ""
        };

        var btn = new Button
        {
            Text      = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColBg2,
            ForeColor = Color.Silver,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(59, 24, 120);
        btn.FlatAppearance.BorderSize  = 2;

        var tips = new ToolTip();
        tips.SetToolTip(btn, tooltip);

        btn.Click += (_, _) =>
        {
            SfxPlayer.PlayClick();
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
            btn.BackColor = active ? ColRed   : ColBg2;
            btn.ForeColor = active ? ColWhite : Color.Silver;
            btn.FlatAppearance.BorderColor = active ? ColRed : Color.FromArgb(59, 24, 120);
        }
    }

    private void HighlightModifiers()
    {
        foreach (var (mod, btn) in _modifierButtons)
        {
            bool on = _selectedModifiers.HasFlag(mod);
            btn.BackColor = on ? ColGreen : ColBg2;
            btn.ForeColor = on ? ColWhite : Color.Silver;
            btn.FlatAppearance.BorderColor = on ? ColGreen : Color.FromArgb(59, 24, 120);
        }
    }

    private void PlayBtn_Click(object? sender, EventArgs e)
    {
        SfxPlayer.PlayClick();
        Hide();
        var gameForm = new GameForm(_selectedDifficulty, _selectedModifiers);
        gameForm.GameFinished += (_, score) => { _finalScore = score; _gameWasStarted = true; };
        gameForm.ShowDialog();
        gameForm.Dispose();
        Close();
    }

    public Difficulty   SelectedDifficulty => _selectedDifficulty;
    public GameModifier SelectedModifiers  => _selectedModifiers;
    public bool         GameWasStarted     => _gameWasStarted;
    public int          FinalScore         => _finalScore;

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _logoTimer?.Stop(); _logoTimer?.Dispose(); }
        base.Dispose(disposing);
    }
}