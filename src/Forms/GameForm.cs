using MATSING.Controls;
using MATSING.Game;
using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Forms;

public class GameForm : Form
{
    // ── Palette ───────────────────────────────────────────────────────────
    private static readonly Color ColBg     = Color.FromArgb( 15,   5,  30);
    private static readonly Color ColBg2    = Color.FromArgb( 32,  10,  62);
    private static readonly Color ColCard   = Color.FromArgb( 48,  18,  92);
    private static readonly Color ColGold   = Color.FromArgb(247, 201,  72);
    private static readonly Color ColRed    = Color.FromArgb(255, 107, 107);
    private static readonly Color ColTeal   = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColGreen  = Color.FromArgb( 80, 200, 120);
    private static readonly Color ColPurple = Color.FromArgb(167, 139, 250);
    private static readonly Color ColWhite  = Color.White;

    // ── Engine ────────────────────────────────────────────────────────────
    private readonly GameEngine  _engine    = new();
    private readonly GameTimer   _timer     = new();
    private readonly Difficulty  _difficulty;
    private readonly GameModifier _modifiers;

    // ── Card controls ─────────────────────────────────────────────────────
    private readonly List<CardControl> _cardControls = new();

    // ── Panels ────────────────────────────────────────────────────────────
    private Panel _topBar      = null!;
    private Panel _timerBar    = null!;  // thin progress bar
    private Panel _timerFill   = null!;
    private Panel _modBadgeBar = null!;  // modifier badge strip
    private Panel _boardPanel  = null!;
    private Panel _bottomBar   = null!;
    private Panel _confettiPanel = null!;

    // ── Top-bar pills (GDI+) ──────────────────────────────────────────────
    private Controls.ScorePillControl _pillMatches = null!,
                                       _pillMoves   = null!,
                                       _pillScore   = null!,
                                       _pillTime    = null!;
    private Label?  _comboLabel;
    private Button  _muteBtn  = null!;

    // ── Toast ─────────────────────────────────────────────────────────────
    private Label  _toastLabel = null!;
    private System.Windows.Forms.Timer _toastTimer    = null!;
    private System.Windows.Forms.Timer _mismatchTimer = null!;

    // ── Bottom status ──────────────────────────────────────────────────────
    private Label _statusLabel = null!;

    // ── Modifier timers ────────────────────────────────────────────────────
    private System.Windows.Forms.Timer? _driftTimer;
    private System.Windows.Forms.Timer? _shrinkTimer;
    private float _shrinkScale = 1.0f;
    private const float SHRINK_MIN  = 0.45f;
    private const float SHRINK_STEP = 0.004f;

    // ── Confetti ──────────────────────────────────────────────────────────
    private readonly List<ConfettiParticle> _confetti = new();
    private System.Windows.Forms.Timer? _confettiTimer;
    private readonly Random _rng = new();

    // ── Grid metrics (for named resize handler) ───────────────────────────
    private int _cols, _cardBaseW, _cardBaseH, _gapX = 14, _gapY = 14;
    private bool _isClosing;
    private bool _beepedThisTick;

    public event EventHandler<int>? GameFinished;

    // ── Constructor ───────────────────────────────────────────────────────
    public GameForm(Difficulty difficulty, GameModifier modifiers = GameModifier.None)
    {
        _difficulty = difficulty;
        _modifiers  = modifiers;

        Text            = "MATSING";
        Size            = new Size(1040, 790);
        MinimumSize     = new Size(820, 660);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon            = SystemIcons.Application;
        FormClosing    += (_, _) => { _isClosing = true; _timer.Stop(); StopModTimers(); };

        _mismatchTimer          = new System.Windows.Forms.Timer { Interval = 900 };
        _mismatchTimer.Tick    += MismatchTimer_Tick;
        _toastTimer             = new System.Windows.Forms.Timer { Interval = 1400 };
        _toastTimer.Tick       += (_, _) => { _toastTimer.Stop(); _toastLabel.Visible = false; };

        BuildUI();
        WireEngine();
        _timer.Tick   += Timer_Tick;
        _timer.TimeUp += Timer_TimeUp;
        StartGame();
    }

    // ── UI Build ─────────────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── Top bar ───────────────────────────────────────────────────────
        _topBar = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = ColBg2 };
        _topBar.Paint += PaintTopBar;

        _pillMatches = new Controls.ScorePillControl("Matches", "0");
        _pillMoves   = new Controls.ScorePillControl("Moves",   "0");
        _pillScore   = new Controls.ScorePillControl("Score",   "0");
        _pillTime    = new Controls.ScorePillControl("Time", _modifiers.HasFlag(GameModifier.ZenMode) ? "∞" : "—");

        var pillFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
        };
        pillFlow.Controls.AddRange(new Control[] { _pillMatches, _pillMoves, _pillScore, _pillTime });

        if (_modifiers.HasFlag(GameModifier.ComboMultiplier))
        {
            _comboLabel = new Label
            {
                Text      = "×1",
                Font      = new Font("Segoe UI Black", 20f, FontStyle.Bold),
                ForeColor = ColGold,
                BackColor = Color.Transparent,
                AutoSize  = true,
            };
            pillFlow.Controls.Add(_comboLabel);
        }

        // Right-side buttons
        _muteBtn = MakeTopBtn("🔊", (_, _) =>
        {
            SfxPlayer.Muted = !SfxPlayer.Muted;
            _muteBtn.Text   = SfxPlayer.Muted ? "🔇" : "🔊";
            if (!SfxPlayer.Muted) SfxPlayer.PlayClick();
        });
        var newBtn  = MakeTopBtn("↺", (_, _) => { SfxPlayer.PlayClick(); StartGame(); });
        var menuBtn = MakeTopBtn("⌂", (_, _) => { SfxPlayer.PlayClick(); Close(); });

        var rightFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
        };
        rightFlow.Controls.AddRange(new Control[] { _muteBtn, newBtn, menuBtn });

        _topBar.Controls.Add(pillFlow);
        _topBar.Controls.Add(rightFlow);
        _topBar.Resize += (_, _) =>
        {
            pillFlow.Location  = new Point((_topBar.Width - pillFlow.PreferredSize.Width) / 2,
                                           (_topBar.Height - pillFlow.PreferredSize.Height) / 2);
            rightFlow.Location = new Point(_topBar.Width - rightFlow.PreferredSize.Width - 10,
                                           (_topBar.Height - rightFlow.PreferredSize.Height) / 2);
        };

        // ── Timer progress bar ────────────────────────────────────────────
        _timerBar = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.FromArgb(25, 255, 255, 255) };
        _timerFill = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = ColTeal };
        _timerBar.Controls.Add(_timerFill);

        // ── Modifier badge strip ──────────────────────────────────────────
        _modBadgeBar = new Panel { Dock = DockStyle.Top, Height = 0, BackColor = ColBg2 };
        _modBadgeBar.Paint += PaintModBadges;
        if (_modifiers != GameModifier.None)
        {
            _modBadgeBar.Height = 30;
        }

        // ── Board ─────────────────────────────────────────────────────────
        _boardPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };
        _boardPanel.Resize += BoardPanel_Resize;

        _toastLabel = new Label
        {
            AutoSize  = false,
            Size      = new Size(260, 46),
            Font      = new Font("Segoe UI Black", 13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false,
            BackColor = Color.Transparent,
            ForeColor = ColWhite,
        };
        _toastLabel.Paint += PaintToast;
        _boardPanel.Controls.Add(_toastLabel);

        // ── Bottom status bar ─────────────────────────────────────────────
        _bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = ColBg2 };
        _bottomBar.Paint += PaintBottomBar;

        _statusLabel = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(190, ColWhite),
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            BackColor = Color.Transparent,
            Text      = BuildStatusText(),
        };
        _bottomBar.Controls.Add(_statusLabel);

        // ── Confetti overlay ──────────────────────────────────────────────
        _confettiPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _confettiPanel.Paint += PaintConfetti;

        Controls.Add(_confettiPanel);
        Controls.Add(_boardPanel);
        Controls.Add(_bottomBar);
        Controls.Add(_modBadgeBar);
        Controls.Add(_timerBar);
        Controls.Add(_topBar);
        _confettiPanel.SendToBack();
    }

    // ── Custom paint: top bar logo ────────────────────────────────────────
    private void PaintTopBar(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Dark gradient strip
        using var bgb = new SolidBrush(ColBg2); g.FillRectangle(bgb, _topBar.ClientRectangle);
        using var lp  = new Pen(Color.FromArgb(40, ColPurple), 1f);
        g.DrawLine(lp, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);

        using var lf = new Font("Segoe UI Black", 16f, FontStyle.Bold, GraphicsUnit.Point);
        using var lb = new SolidBrush(ColGold);
        g.DrawString("🐾 MATSING", lf, lb, 12, (_topBar.Height - 22) / 2);
    }

    // ── Custom paint: modifier badge strip ───────────────────────────────
    private void PaintModBadges(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bgb = new SolidBrush(ColBg2); g.FillRectangle(bgb, _modBadgeBar.ClientRectangle);

        var badges = new List<(string text, Color col)>();
        if (_modifiers.HasFlag(GameModifier.ZenMode))         badges.Add(("🧘 Zen",      ColTeal));
        if (_modifiers.HasFlag(GameModifier.HardcoreMode))    badges.Add(("💀 Hardcore", ColRed));
        if (_modifiers.HasFlag(GameModifier.TripleMatch))     badges.Add(("🎲 Triple",   ColPurple));
        if (_modifiers.HasFlag(GameModifier.CardDrift))       badges.Add(("🌀 Drift",    ColTeal));
        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))  badges.Add(("📉 Shrink",   ColGold));
        if (_modifiers.HasFlag(GameModifier.FlipLimit))       badges.Add(("🔒 Flip ×2",  ColRed));
        if (_modifiers.HasFlag(GameModifier.ComboMultiplier)) badges.Add(("⚡ Combo",    ColGold));

        using var bf = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
        float bx = 10;
        foreach (var (txt, col) in badges)
        {
            var tsz = g.MeasureString(txt, bf);
            var r   = new RectangleF(bx, 4, tsz.Width + 14, 22);
            using var bb  = new SolidBrush(Color.FromArgb(40, col));
            using var bp  = new Pen(Color.FromArgb(100, col), 1f);
            using var btb = new SolidBrush(col);
            g.FillRectangle(bb, r);
            g.DrawRectangle(bp, r.X, r.Y, r.Width, r.Height);
            g.DrawString(txt, bf, btb, bx + 7, r.Y + 3);
            bx += r.Width + 6;
        }
    }

    // ── Custom paint: bottom bar ──────────────────────────────────────────
    private void PaintBottomBar(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        using var bgb = new SolidBrush(ColBg2); g.FillRectangle(bgb, _bottomBar.ClientRectangle);
        using var lp  = new Pen(Color.FromArgb(35, ColPurple), 1f);
        g.DrawLine(lp, 0, 0, _bottomBar.Width, 0);
    }

    // ── Custom paint: toast ────────────────────────────────────────────────
    private void PaintToast(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, _toastLabel.Width, _toastLabel.Height);
        using var bb = new SolidBrush(Color.FromArgb(220, ColCard)); FillRound(g, bb, r, 12);
        using var bp = new Pen(Color.FromArgb(140, ColGold), 1.5f);  DrawRound(g, bp, r, 12);
        using var tf = new Font("Segoe UI Black", 13f, FontStyle.Bold, GraphicsUnit.Point);
        using var tb = new SolidBrush(ColGold);
        var tsz = g.MeasureString(_toastLabel.Text, tf);
        g.DrawString(_toastLabel.Text, tf, tb,
            (r.Width - tsz.Width) / 2f, (r.Height - tsz.Height) / 2f);
    }

    // ── Top-bar button factory ────────────────────────────────────────────
    private Button MakeTopBtn(string text, EventHandler click)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(59, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, ColPurple),
            ForeColor = ColWhite,
            Font      = new Font("Segoe UI Emoji", 14f),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(5, 0, 0, 0),
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(60, ColPurple);
        btn.FlatAppearance.BorderSize  = 1;
        btn.MouseEnter += (_, _) => btn.BackColor = Color.FromArgb(100, ColPurple);
        btn.MouseLeave += (_, _) => btn.BackColor = Color.FromArgb(50,  ColPurple);
        btn.Click += click;
        return btn;
    }

    // ── Engine wiring ─────────────────────────────────────────────────────
    private void WireEngine()
    {
        _engine.MatchFound       += Engine_MatchFound;
        _engine.MismatchFound    += Engine_MismatchFound;
        _engine.GameWon          += Engine_GameWon;
        _engine.GameOverHardcore += Engine_GameOverHardcore;
        _engine.CardsDrifted     += Engine_CardsDrifted;
    }

    // ── Start / Restart ───────────────────────────────────────────────────
    private void StartGame()
    {
        _mismatchTimer.Stop();
        StopModTimers();
        _confettiTimer?.Stop(); _confettiTimer?.Dispose(); _confettiTimer = null;
        _confetti.Clear(); _confettiPanel.SendToBack();
        _timer.Reset();
        _shrinkScale = 1.0f;

        _engine.StartNewGame(_difficulty, _modifiers);
        BuildCardGrid();
        UpdatePills();
        _statusLabel.Text       = BuildStatusText();
        _timerFill.BackColor    = ColTeal;
        _timerBar.Visible       = !_modifiers.HasFlag(GameModifier.ZenMode);

        if (_modifiers.HasFlag(GameModifier.ZenMode)) _pillTime.SetValue("∞");
        else _timer.Start(_engine.Config.TimeLimitS);

        StartModTimers();
        _modBadgeBar.Invalidate();
    }

    // ── Card grid ─────────────────────────────────────────────────────────
    private void BuildCardGrid()
    {
        foreach (var c in _cardControls) { c.CardClicked -= Card_Clicked; c.Dispose(); }
        _cardControls.Clear();
        _boardPanel.Controls.Clear();
        _boardPanel.Controls.Add(_toastLabel);

        var cfg  = _engine.Config;
        var deck = _engine.GetDeck();
        _cols      = cfg.Columns;
        _cardBaseW = cfg.CardWidth;
        _cardBaseH = cfg.CardHeight;

        for (int i = 0; i < deck.Count; i++)
        {
            CardControl ctrl = deck[i] switch
            {
                MonkeyCard  m => new MonkeyCardControl(m),
                SpecialCard s => new SpecialCardControl(s),
                _             => new CardControl(deck[i]),
            };
            ctrl.Size        = new Size(_cardBaseW, _cardBaseH);
            ctrl.CardClicked += Card_Clicked;
            _cardControls.Add(ctrl);
            _boardPanel.Controls.Add(ctrl);
        }

        if (_modifiers.HasFlag(GameModifier.FlipLimit)) AttachFlipOverlays();
        RepositionCards();
        _toastLabel.BringToFront();
    }

    private void AttachFlipOverlays()
    {
        foreach (var ctrl in _cardControls)
        {
            ctrl.Paint += (_, pe) =>
            {
                int rem = _engine.FlipsRemaining(ctrl.CardData);
                if (rem < 0 || ctrl.CardData.IsMatched) return;
                using var f  = new Font("Segoe UI Black", Math.Max(7f, ctrl.Width / 10f), FontStyle.Bold, GraphicsUnit.Point);
                using var bb = new SolidBrush(rem == 0 ? Color.FromArgb(200, ColRed) : Color.FromArgb(150, ColBg2));
                using var fb = new SolidBrush(Color.White);
                string t     = rem.ToString();
                var    sz    = pe.Graphics.MeasureString(t, f);
                var    r     = new RectangleF(ctrl.Width - sz.Width - 6, 4, sz.Width + 4, sz.Height + 2);
                pe.Graphics.FillRectangle(bb, r);
                pe.Graphics.DrawString(t, f, fb, r.X + 2, r.Y + 1);
            };
        }
    }

    private void BoardPanel_Resize(object? sender, EventArgs e) => RepositionCards();

    private void RepositionCards()
    {
        if (_cardControls.Count == 0) return;
        int cW = _cardControls[0].Width, cH = _cardControls[0].Height;
        int gW = _cols * cW + (_cols - 1) * _gapX;
        int rows = (int)Math.Ceiling(_cardControls.Count / (double)_cols);
        int gH   = rows * cH + (rows - 1) * _gapY;
        int offX = Math.Max(10, (_boardPanel.Width  - gW) / 2);
        int offY = Math.Max(10, (_boardPanel.Height - gH) / 2);
        for (int i = 0; i < _cardControls.Count; i++)
        {
            int col = i % _cols, row = i / _cols;
            _cardControls[i].Left = offX + col * (cW + _gapX);
            _cardControls[i].Top  = offY + row * (cH + _gapY);
        }
        _toastLabel.Location = new Point(
            (_boardPanel.Width  - _toastLabel.Width)  / 2,
            (_boardPanel.Height - _toastLabel.Height) / 2 + 60);
    }

    // ── Card click ────────────────────────────────────────────────────────
    private void Card_Clicked(object? sender, CardBase card)
    {
        if (sender is not CardControl ctrl) return;
        bool flipBlocked = _modifiers.HasFlag(GameModifier.FlipLimit)
                        && !card.IsMatched && !card.IsFaceUp
                        && _engine.FlipsRemaining(card) <= 0;
        bool accepted = _engine.OnCardSelected(card, _timer.Elapsed);
        if (!accepted) { if (flipBlocked) SfxPlayer.PlayFlipLimit(); return; }
        SfxPlayer.PlayFlip();
        ctrl.PlayFlipAnimation();
        ctrl.Invalidate();
    }

    // ── Match ─────────────────────────────────────────────────────────────
    private void Engine_MatchFound(object? sender, CardPairEventArgs e)
    {
        FindCtrl(e.First)?.PlayMatchAnimation();
        FindCtrl(e.Second)?.PlayMatchAnimation();
        if (e.Third != null) FindCtrl(e.Third)?.PlayMatchAnimation();

        // Grow cards temporarily in shrinking mode when match found
        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))
        {
            GrowCardsTemporarily();
        }

        int streak = _engine.Streak;
        if (streak >= 2)
        {
            SfxPlayer.PlayCombo(streak);
            string txt = _modifiers.HasFlag(GameModifier.ComboMultiplier)
                ? $"🔥 {streak}× Combo! ×{Math.Min(streak, 4)} pts"
                : $"🔥 {streak}× Combo!";
            ShowToast(txt);
            _statusLabel.Text = $"🔥 {streak}× Combo Streak!";
        }
        else
        {
            SfxPlayer.PlayMatch();
            ShowToast("🎯 Match!");
            _statusLabel.Text = $"Matched  {_engine.MatchCount} / {_engine.TotalPairs}  pairs";
        }
        UpdatePills();
        UpdateComboLabel();
    }

    // ── Mismatch ──────────────────────────────────────────────────────────
    private void Engine_MismatchFound(object? sender, CardPairEventArgs e)
    {
        SfxPlayer.PlayMismatch();
        _statusLabel.Text = "Not a match — keep looking! 🙈";
        UpdatePills(); UpdateComboLabel();
        if (_modifiers.HasFlag(GameModifier.FlipLimit))
        {
            FindCtrl(e.First)?.Invalidate();
            FindCtrl(e.Second)?.Invalidate();
            if (e.Third != null) FindCtrl(e.Third)?.Invalidate();
        }
        _mismatchTimer.Start();
    }

    private void MismatchTimer_Tick(object? sender, EventArgs e)
    {
        _mismatchTimer.Stop();
        _engine.FlipDownMismatch();
        SfxPlayer.PlayFlipBack();
        foreach (var c in _cardControls) if (!c.CardData.IsMatched) c.Invalidate();
    }

    // ── Hardcore over ─────────────────────────────────────────────────────
    private void Engine_GameOverHardcore(object? sender, EventArgs e)
    {
        if (_isClosing) return;
        _timer.Stop(); StopModTimers();
        SfxPlayer.PlayGameOver();
        ShowToast("💀 Hardcore: Game Over!");
        _statusLabel.Text = "One wrong flip ended the run! 💀";
        Task.Delay(1000).ContinueWith(_ =>
        {
            if (IsDisposed || _isClosing) return;
            Invoke(() =>
            {
                var wf = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score, _difficulty, timeUp: true);
                wf.ShowDialog(this);
                GameFinished?.Invoke(this, _engine.Score);
                if (wf.PlayAgain) StartGame(); else Close();
            });
        });
    }

    // ── Drift ─────────────────────────────────────────────────────────────
    private void Engine_CardsDrifted(object? sender, DriftEventArgs e)
    {
        if (_isClosing) return;
        var lookup = new Dictionary<CardBase, CardControl>(ReferenceEqualityComparer.Instance);
        foreach (var c in _cardControls) lookup[c.CardData] = c;
        _cardControls.Clear();
        foreach (var card in e.NewOrder)
            if (lookup.TryGetValue(card, out var ctrl)) _cardControls.Add(ctrl);
        SfxPlayer.PlayShuffle();
        ShowToast("🌀 Cards Drifted!");
        RepositionCards();
    }

    // ── Win ───────────────────────────────────────────────────────────────
    private void Engine_GameWon(object? sender, EventArgs e)
    {
        _timer.Stop(); StopModTimers();
        SfxPlayer.PlayGameWin();
        LaunchConfetti();
        var wf = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score, _difficulty);
        wf.ShowDialog(this);
        GameFinished?.Invoke(this, _engine.Score);
        if (wf.PlayAgain) StartGame(); else Close();
    }

    // ── Timer ─────────────────────────────────────────────────────────────
    private void Timer_Tick(object? sender, TimerTickEventArgs e)
    {
        _pillTime.SetValue($"{e.Remaining}s");
        _timerFill.Width     = Math.Max(0, (int)(_timerBar.Width * e.RemainingFraction));
        _timerFill.BackColor = e.RemainingFraction < 0.3f ? ColRed : ColTeal;

        if (e.Remaining <= 10 && e.Remaining > 0)
        { if (!_beepedThisTick) { SfxPlayer.PlayCountdownBeep(); _beepedThisTick = true; } }
        else { _beepedThisTick = false; }
    }

    private void Timer_TimeUp(object? sender, EventArgs e)
    {
        if (_isClosing) return;
        _pillTime.SetValue("0s");
        SfxPlayer.PlayGameOver();
        ShowToast("⏰ Time's Up!");
        _statusLabel.Text = "Time's up! Better luck next time 🙈";
        StopModTimers();
        var wf = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score, _difficulty, timeUp: true);
        wf.ShowDialog(this);
        GameFinished?.Invoke(this, _engine.Score);
        if (wf.PlayAgain) StartGame(); else Close();
    }

    // ── Modifier timers ────────────────────────────────────────────────────
    private void StartModTimers()
    {
        if (_modifiers.HasFlag(GameModifier.CardDrift))
        {
            _driftTimer = new System.Windows.Forms.Timer { Interval = 20_000 };
            _driftTimer.Tick += (_, _) => { if (!_isClosing) _engine.TriggerCardDrift(); };
            _driftTimer.Start();
        }
        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))
        {
            _shrinkTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _shrinkTimer.Tick += (_, _) =>
            {
                if (_isClosing) return;
                _shrinkScale = Math.Max(SHRINK_MIN, _shrinkScale - SHRINK_STEP);
                ApplyShrink();
            };
            _shrinkTimer.Start();
        }
    }

    private void StopModTimers()
    {
        _driftTimer?.Stop();  _driftTimer?.Dispose();  _driftTimer  = null;
        _shrinkTimer?.Stop(); _shrinkTimer?.Dispose(); _shrinkTimer = null;
    }

    private void ApplyShrink()
    {
        if (_cardBaseW == 0) return;
        int nW = Math.Max(40, (int)(_cardBaseW * _shrinkScale));
        int nH = Math.Max(50, (int)(_cardBaseH * _shrinkScale));
        foreach (var c in _cardControls) c.Size = new Size(nW, nH);
        RepositionCards();
    }

    private void GrowCardsTemporarily()
    {
        // Temporarily increase shrink scale when a match is found
        _shrinkScale = Math.Min(2.0f, _shrinkScale + 0.05f);
        ApplyShrink();
    }

    // ── Confetti ──────────────────────────────────────────────────────────
    private void LaunchConfetti()
    {
        _confetti.Clear();
        Color[] pal = { ColGold, ColRed, ColTeal, ColWhite, ColPurple };
        for (int i = 0; i < 180; i++)
            _confetti.Add(new ConfettiParticle(
                _rng.Next(0, Width), _rng.Next(-Height, 0),
                _rng.Next(4, 12), (float)(_rng.NextDouble() * 3 + 1),
                pal[_rng.Next(pal.Length)], (float)(_rng.NextDouble() * 0.15 - 0.075)));

        _confettiPanel.BringToFront();
        _confettiTimer = new System.Windows.Forms.Timer { Interval = 25 };
        _confettiTimer.Tick += (_, _) =>
        {
            bool any = false;
            foreach (var p in _confetti) { p.Y += p.Speed; p.X += (float)Math.Sin(p.Y * p.Tilt) * 2; if (p.Y < Height + 20) any = true; }
            _confettiPanel.Invalidate();
            if (!any) { _confettiTimer?.Stop(); _confettiTimer?.Dispose(); _confettiTimer = null; _confettiPanel.SendToBack(); }
        };
        _confettiTimer.Start();
    }

    private void PaintConfetti(object? sender, PaintEventArgs e)
    {
        foreach (var p in _confetti)
        { using var b = new SolidBrush(p.Colour); e.Graphics.FillRectangle(b, p.X, p.Y, p.Size, p.Size * 0.5f); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void ShowToast(string msg)
    {
        _toastLabel.Text    = msg;
        _toastLabel.Visible = true;
        _toastLabel.BringToFront();
        _toastLabel.Invalidate();
        _toastTimer.Stop(); _toastTimer.Start();
    }

    private void UpdatePills()
    {
        _pillMatches.SetValue(_engine.MatchCount.ToString());
        _pillMoves  .SetValue(_engine.MoveCount .ToString());
        _pillScore  .SetValue(_engine.Score     .ToString());
    }

    private void UpdateComboLabel()
    {
        if (_comboLabel == null) return;
        int m = Math.Min(_engine.Streak, 4);
        _comboLabel.Text      = $"×{Math.Max(1, m)}";
        _comboLabel.ForeColor = m >= 4 ? ColRed : m >= 2 ? ColGold : ColTeal;
    }

    private string BuildStatusText()
    {
        var parts = new List<string>();
        if (_modifiers.HasFlag(GameModifier.ZenMode))         parts.Add("🧘 Zen");
        if (_modifiers.HasFlag(GameModifier.HardcoreMode))    parts.Add("💀 Hardcore");
        if (_modifiers.HasFlag(GameModifier.TripleMatch))     parts.Add("🎲 Triple");
        if (_modifiers.HasFlag(GameModifier.CardDrift))       parts.Add("🌀 Drift");
        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))  parts.Add("📉 Shrink");
        if (_modifiers.HasFlag(GameModifier.FlipLimit))       parts.Add("🔒 Flip×2");
        if (_modifiers.HasFlag(GameModifier.ComboMultiplier)) parts.Add("⚡ Combo");
        string mods = parts.Count > 0 ? "  │  " + string.Join("  ", parts) : "";
        return $"Find the matching monkey pairs! 🐒{mods}";
    }

    private CardControl? FindCtrl(CardBase card) =>
        _cardControls.FirstOrDefault(c => ReferenceEquals(c.CardData, card));

    // ── GDI+ helpers ─────────────────────────────────────────────────────
    private static void FillRound(Graphics g, Brush b, Rectangle r, int rad)
    { using var p = RoundPath(r, rad); g.FillPath(b, p); }
    private static void DrawRound(Graphics g, Pen pen, Rectangle r, int rad)
    { using var p = RoundPath(r, rad); g.DrawPath(pen, p); }
    private static System.Drawing.Drawing2D.GraphicsPath RoundPath(Rectangle r, int rad)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = Math.Max(1, Math.Min(rad * 2, Math.Min(r.Width, r.Height)));
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose(); _mismatchTimer.Dispose(); _toastTimer.Dispose();
            StopModTimers();
            _confettiTimer?.Stop(); _confettiTimer?.Dispose();
            foreach (var c in _cardControls) c.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal class ConfettiParticle
{
    public float X, Y, Speed, Tilt, Size; public Color Colour;
    public ConfettiParticle(float x, float y, float sz, float sp, Color c, float t)
    { X=x; Y=y; Size=sz; Speed=sp; Colour=c; Tilt=t; }
}