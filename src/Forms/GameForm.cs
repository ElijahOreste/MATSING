using MATSING.Controls;
using MATSING.Game;
using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// The main game board form for MATSING.<br/>
/// Demonstrates <b>POLYMORPHISM IN ACTION</b>: holds a
/// <c>List&lt;CardControl&gt;</c> and calls virtual animation methods —
/// runtime dispatch routes to the correct override per card type.
/// </summary>
public class GameForm : Form
{
    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColBg    = Color.FromArgb( 26,  10,  46);
    private static readonly Color ColBg2   = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold  = Color.FromArgb(247, 201,  72);
    private static readonly Color ColTeal  = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColRed   = Color.FromArgb(255, 107, 107);

    // ── Engine & Timer ────────────────────────────────────────────────────
    private readonly GameEngine _engine     = new();
    private readonly GameTimer  _timer      = new();
    private readonly Difficulty _difficulty;
    private readonly GameModifier _modifiers;

    // ═══════════════════════════════════════════════════════════════
    //  POLYMORPHISM IN ACTION
    //  This list holds CardControl references — at runtime some are
    //  MonkeyCardControl, some SpecialCardControl.
    // ═══════════════════════════════════════════════════════════════
    private readonly List<CardControl> _cardControls = new();

    // ── UI Controls ───────────────────────────────────────────────────────
    private Panel              _topBar        = null!;
    private Panel              _timerBarWrap  = null!;
    private Panel              _timerBarFill  = null!;
    private Panel              _boardPanel    = null!;
    private Panel              _bottomBar     = null!;
    private Controls.ScorePillControl _pillMatches = null!, _pillMoves = null!,
                                       _pillScore  = null!, _pillTime  = null!;
    private Label              _streakLabel   = null!;
    private Label              _toastLabel    = null!;
    private System.Windows.Forms.Timer _toastTimer    = null!;
    private System.Windows.Forms.Timer _mismatchTimer = null!;

    // ── Modifier Timers ───────────────────────────────────────────────────
    private System.Windows.Forms.Timer? _driftTimer;
    private System.Windows.Forms.Timer? _shrinkTimer;
    private float _shrinkScale = 1.0f;
    private const float SHRINK_MIN   = 0.45f;
    private const float SHRINK_STEP  = 0.004f;

    // ── Confetti ──────────────────────────────────────────────────────────
    private List<ConfettiParticle> _confetti = new();
    private System.Windows.Forms.Timer? _confettiTimer;
    private Panel _confettiPanel = null!;
    private readonly Random _rng = new();

    // ── Combo label ───────────────────────────────────────────────────────
    private Label? _comboLabel;

    // ── Form State ────────────────────────────────────────────────────────
    private bool _isClosing = false;
    // Cached grid metrics used by the resize handler (no lambda capture accumulation)
    private int _gridW, _gridH, _cardBaseW, _cardBaseH, _gapX, _gapY, _cols;

    // ── Event ─────────────────────────────────────────────────────────────
    /// <summary>Raised when the game ends (win or time-up). Carries final score.</summary>
    public event EventHandler<int>? GameFinished;

    // ── Constructor ───────────────────────────────────────────────────────
    public GameForm(Difficulty difficulty, GameModifier modifiers = GameModifier.None)
    {
        _difficulty = difficulty;
        _modifiers  = modifiers;
        InitialiseForm();
        BuildUI();
        WireEngineEvents();
        WireTimerEvents();
        StartGame();
    }

    // ── Form Setup ────────────────────────────────────────────────────────
    private void InitialiseForm()
    {
        Text            = "MATSING – In Game";
        Size            = new Size(1000, 760);
        MinimumSize     = new Size(800, 650);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon            = SystemIcons.Application;
        FormClosing     += GameForm_FormClosing;

        _mismatchTimer = new System.Windows.Forms.Timer { Interval = 900 };
        _mismatchTimer.Tick += MismatchTimer_Tick;

        _toastTimer = new System.Windows.Forms.Timer { Interval = 1400 };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toastLabel.Visible = false;
        };
    }

    // ── UI Construction ───────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── Top bar ───────────────────────────────────────────────────────
        _topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 80,
            BackColor = ColBg2,
            Padding   = new Padding(14, 8, 14, 8),
        };
        _topBar.Paint += PaintTopBar;

        _pillMatches = new Controls.ScorePillControl("Matches", "0");
        _pillMoves   = new Controls.ScorePillControl("Moves",   "0");
        _pillScore   = new Controls.ScorePillControl("Score",   "0");
        _pillTime    = new Controls.ScorePillControl("Time",    _modifiers.HasFlag(GameModifier.ZenMode) ? "∞" : "—");

        var pillFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        pillFlow.Controls.AddRange(new Control[]
            { _pillMatches, _pillMoves, _pillScore, _pillTime });

        // ComboMultiplier pill-style label
        if (_modifiers.HasFlag(GameModifier.ComboMultiplier))
        {
            _comboLabel = new Label
            {
                Text      = "×1",
                Font      = new Font("Segoe UI Black", 22f, FontStyle.Bold),
                ForeColor = ColGold,
                BackColor = Color.Transparent,
                AutoSize  = true,
            };
            pillFlow.Controls.Add(_comboLabel);
        }

        // Right-side buttons
        var newBtn  = MakeTopButton("↺ New",  (_, _) => StartGame());
        var menuBtn = MakeTopButton("⌂ Menu", (_, _) => Close());

        var rightFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
        };
        rightFlow.Controls.AddRange(new Control[] { newBtn, menuBtn });

        _topBar.Controls.Add(pillFlow);
        _topBar.Controls.Add(rightFlow);
        _topBar.Resize += (_, _) =>
        {
            pillFlow.Location  = new Point(
                (_topBar.Width - pillFlow.PreferredSize.Width) / 2,
                (_topBar.Height - pillFlow.PreferredSize.Height) / 2);
            rightFlow.Location = new Point(
                _topBar.Width - rightFlow.PreferredSize.Width - 10,
                (_topBar.Height - rightFlow.PreferredSize.Height) / 2);
        };

        // ── Timer progress bar ────────────────────────────────────────────
        _timerBarWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 10,
            BackColor = Color.FromArgb(30, 255, 255, 255),
        };
        _timerBarFill = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = _timerBarWrap.Width,
            BackColor = ColTeal,
        };
        _timerBarWrap.Controls.Add(_timerBarFill);

        // ── Bottom bar ────────────────────────────────────────────────────
        _bottomBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 42,
            BackColor = ColBg2,
        };
        _streakLabel = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = ColGold,
            Font      = new Font("Segoe UI Black", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Text      = BuildStatusText(),
        };
        _bottomBar.Controls.Add(_streakLabel);

        // ── Board panel ───────────────────────────────────────────────────
        _boardPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll= true,
        };
        // FIX: use a named handler to avoid accumulation on restarts
        _boardPanel.Resize += BoardPanel_Resize;

        // ── Toast label (floating feedback) ──────────────────────────────
        _toastLabel = new Label
        {
            AutoSize  = false,
            Size      = new Size(240, 44),
            ForeColor = ColBg,
            BackColor = ColGold,
            Font      = new Font("Segoe UI Black", 13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false,
        };
        _boardPanel.Controls.Add(_toastLabel);

        // ── Confetti overlay panel ────────────────────────────────────────
        _confettiPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
        };
        _confettiPanel.Paint += PaintConfetti;

        // ── Add to form ───────────────────────────────────────────────────
        Controls.Add(_confettiPanel);
        Controls.Add(_boardPanel);
        Controls.Add(_bottomBar);
        Controls.Add(_timerBarWrap);
        Controls.Add(_topBar);

        _confettiPanel.BringToFront();
        _confettiPanel.SendToBack();
    }

    // ── Engine Events ─────────────────────────────────────────────────────
    private void WireEngineEvents()
    {
        _engine.MatchFound       += Engine_MatchFound;
        _engine.MismatchFound    += Engine_MismatchFound;
        _engine.GameWon          += Engine_GameWon;
        _engine.GameOverHardcore += Engine_GameOverHardcore;
        _engine.CardsDrifted     += Engine_CardsDrifted;
    }

    private void WireTimerEvents()
    {
        _timer.Tick   += Timer_Tick;
        _timer.TimeUp += Timer_TimeUp;
    }

    // ── Start / Restart ───────────────────────────────────────────────────
    private void StartGame()
    {
        _mismatchTimer.Stop();
        StopModifierTimers();

        _confettiTimer?.Stop();
        _confettiTimer?.Dispose();
        _confettiTimer = null;
        _confetti.Clear();
        _confettiPanel.SendToBack();

        _timer.Reset();
        _shrinkScale = 1.0f;

        _engine.StartNewGame(_difficulty, _modifiers);
        BuildCardGrid();
        UpdatePills();
        _streakLabel.Text = BuildStatusText();
        _timerBarFill.BackColor = ColTeal;

        if (_modifiers.HasFlag(GameModifier.ZenMode))
        {
            // Zen: no countdown — hide the bar entirely
            _timerBarWrap.Visible = false;
            _pillTime.SetValue("∞");
        }
        else
        {
            _timerBarWrap.Visible = true;
            _timer.Start(_engine.Config.TimeLimitS);
        }

        StartModifierTimers();
    }

    // ── Modifier Timer Management ─────────────────────────────────────────
    private void StartModifierTimers()
    {
        if (_modifiers.HasFlag(GameModifier.CardDrift))
        {
            _driftTimer = new System.Windows.Forms.Timer { Interval = 20_000 };
            _driftTimer.Tick += (_, _) =>
            {
                if (!_isClosing) _engine.TriggerCardDrift();
            };
            _driftTimer.Start();
        }

        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))
        {
            _shrinkTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _shrinkTimer.Tick += (_, _) =>
            {
                if (_isClosing) return;
                _shrinkScale = Math.Max(SHRINK_MIN, _shrinkScale - SHRINK_STEP);
                ApplyShrink();
            };
            _shrinkTimer.Start();
        }
    }

    private void StopModifierTimers()
    {
        _driftTimer?.Stop();
        _driftTimer?.Dispose();
        _driftTimer = null;

        _shrinkTimer?.Stop();
        _shrinkTimer?.Dispose();
        _shrinkTimer = null;
    }

    private void ApplyShrink()
    {
        int baseW = _cardBaseW;
        int baseH = _cardBaseH;
        if (baseW == 0 || baseH == 0) return;

        int newW = Math.Max(40, (int)(baseW * _shrinkScale));
        int newH = Math.Max(50, (int)(baseH * _shrinkScale));

        foreach (var ctrl in _cardControls)
            ctrl.Size = new Size(newW, newH);

        RepositionCards();
    }

    // ── Build Card Grid ───────────────────────────────────────────────────
    private void BuildCardGrid()
    {
        // Dispose old controls
        foreach (var c in _cardControls) { c.CardClicked -= Card_Clicked; c.Dispose(); }
        _cardControls.Clear();
        _boardPanel.Controls.Clear();
        _boardPanel.Controls.Add(_toastLabel);

        var cfg  = _engine.Config;
        var deck = _engine.GetDeck();

        _cols      = cfg.Columns;
        _cardBaseW = cfg.CardWidth;
        _cardBaseH = cfg.CardHeight;
        _gapX      = 14;
        _gapY      = 14;
        int rows   = (int)Math.Ceiling((double)deck.Count / _cols);
        _gridW     = _cols * _cardBaseW + (_cols - 1) * _gapX;
        _gridH     = rows  * _cardBaseH + (rows  - 1) * _gapY;

        for (int i = 0; i < deck.Count; i++)
        {
            var card = deck[i];

            // ═══════════════════════════════════════════════════════════════
            //  POLYMORPHISM IN ACTION — factory creates the correct subtype
            // ═══════════════════════════════════════════════════════════════
            CardControl ctrl = card switch
            {
                MonkeyCard  m => new MonkeyCardControl(m),
                SpecialCard s => new SpecialCardControl(s),
                _             => new CardControl(card),
            };

            ctrl.Size        = new Size(_cardBaseW, _cardBaseH);
            ctrl.CardClicked += Card_Clicked;

            _cardControls.Add(ctrl);
            _boardPanel.Controls.Add(ctrl);
        }

        // Show FlipLimit overlay if active
        if (_modifiers.HasFlag(GameModifier.FlipLimit))
            AttachFlipLimitOverlays();

        RepositionCards();
        _toastLabel.BringToFront();
    }

    // Attach a small flip-count badge to each card when FlipLimit is active
    private void AttachFlipLimitOverlays()
    {
        foreach (var ctrl in _cardControls)
        {
            ctrl.Paint += (_, e) =>
            {
                int rem = _engine.FlipsRemaining(ctrl.CardData);
                if (rem < 0 || ctrl.CardData.IsMatched) return;

                string txt = rem.ToString();
                using var f = new Font("Segoe UI Black", Math.Max(7f, ctrl.Width / 10f), FontStyle.Bold, GraphicsUnit.Point);
                using var bg = new SolidBrush(rem == 0 ? Color.FromArgb(200, ColRed) : Color.FromArgb(160, ColBg2));
                using var fg = new SolidBrush(Color.White);
                var sz = e.Graphics.MeasureString(txt, f);
                var r = new RectangleF(ctrl.Width - sz.Width - 6, 4, sz.Width + 4, sz.Height + 2);
                e.Graphics.FillRectangle(bg, r);
                e.Graphics.DrawString(txt, f, fg, r.X + 2, r.Y + 1);
            };
        }
    }

    // FIX: named method — called once per Resize, no lambda accumulation
    private void BoardPanel_Resize(object? sender, EventArgs e) => RepositionCards();

    private void RepositionCards()
    {
        if (_cardControls.Count == 0) return;
        int cardW = _cardControls[0].Width;
        int cardH = _cardControls[0].Height;
        int gridW = _cols * cardW + (_cols - 1) * _gapX;
        int gridH = (int)Math.Ceiling((double)_cardControls.Count / _cols) * cardH
                  + ((int)Math.Ceiling((double)_cardControls.Count / _cols) - 1) * _gapY;

        int offX  = Math.Max(10, (_boardPanel.Width  - gridW) / 2);
        int offY  = Math.Max(10, (_boardPanel.Height - gridH) / 2);

        for (int i = 0; i < _cardControls.Count; i++)
        {
            int col = i % _cols, row = i / _cols;
            _cardControls[i].Left = offX + col * (cardW + _gapX);
            _cardControls[i].Top  = offY + row * (cardH + _gapY);
        }

        // Centre toast
        _toastLabel.Location = new Point(
            (_boardPanel.Width  - _toastLabel.Width)  / 2,
            (_boardPanel.Height - _toastLabel.Height) / 2 + 60);
    }

    // ── Card Click Handler ────────────────────────────────────────────────
    private void Card_Clicked(object? sender, CardBase card)
    {
        if (sender is not CardControl ctrl) return;
        bool accepted = _engine.OnCardSelected(card, _timer.Elapsed);
        if (!accepted) return;

        // POLYMORPHISM: ctrl is MonkeyCardControl OR SpecialCardControl
        ctrl.PlayFlipAnimation();
        ctrl.Invalidate(); // refresh FlipLimit overlay
    }

    // ── Match Found ───────────────────────────────────────────────────────
    private void Engine_MatchFound(object? sender, CardPairEventArgs e)
    {
        var a = FindControl(e.First);
        var b = FindControl(e.Second);
        var c = e.Third != null ? FindControl(e.Third) : null;

        a?.PlayMatchAnimation();
        b?.PlayMatchAnimation();
        c?.PlayMatchAnimation();

        int streak = _engine.Streak;
        if (streak >= 2)
        {
            string comboText = _modifiers.HasFlag(GameModifier.ComboMultiplier)
                ? $"🔥 {streak}x Combo! ×{_engine.Score}" // show multiplier feedback
                : $"🔥 {streak}x Combo!";
            ShowToast(comboText);
            _streakLabel.Text = $"🔥 {streak}x Combo Streak!";
        }
        else
        {
            ShowToast("🎯 Match!");
            _streakLabel.Text = $"Matched: {_engine.MatchCount} / {_engine.TotalPairs}";
        }

        UpdatePills();
        UpdateComboLabel();
    }

    // ── Mismatch Found ────────────────────────────────────────────────────
    private void Engine_MismatchFound(object? sender, CardPairEventArgs e)
    {
        UpdatePills();
        _streakLabel.Text = "Not a match — keep looking! 🙈";
        UpdateComboLabel();

        // FlipLimit: invalidate affected cards to refresh their overlays
        if (_modifiers.HasFlag(GameModifier.FlipLimit))
        {
            FindControl(e.First)?.Invalidate();
            FindControl(e.Second)?.Invalidate();
            if (e.Third != null) FindControl(e.Third)?.Invalidate();
        }

        _mismatchTimer.Start();
    }

    private void MismatchTimer_Tick(object? sender, EventArgs e)
    {
        _mismatchTimer.Stop();
        _engine.FlipDownMismatch();

        foreach (var ctrl in _cardControls)
            if (!ctrl.CardData.IsMatched) ctrl.Invalidate();
    }

    // ── Hardcore Game Over ────────────────────────────────────────────────
    private void Engine_GameOverHardcore(object? sender, EventArgs e)
    {
        if (_isClosing) return;
        _timer.Stop();
        StopModifierTimers();

        ShowToast("💀 Hardcore: Game Over!");
        _streakLabel.Text = "One wrong flip ended the run! 💀";

        // Brief pause so the player sees the mismatch before the dialog
        Task.Delay(1000).ContinueWith(_ =>
        {
            if (IsDisposed || _isClosing) return;
            Invoke(() =>
            {
                var winForm = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score,
                                          _difficulty, timeUp: true);
                winForm.ShowDialog(this);
                GameFinished?.Invoke(this, _engine.Score);
                if (winForm.PlayAgain) StartGame();
                else Close();
            });
        });
    }

    // ── CardDrift ─────────────────────────────────────────────────────────
    private void Engine_CardsDrifted(object? sender, DriftEventArgs e)
    {
        if (_isClosing) return;

        // Remap controls to the new deck order
        var newOrder = e.NewOrder;
        var oldControls = _cardControls.ToList();

        // Build a lookup from CardBase → control
        var lookup = new Dictionary<CardBase, CardControl>(ReferenceEqualityComparer.Instance);
        foreach (var ctrl in oldControls)
            lookup[ctrl.CardData] = ctrl;

        // Rebuild _cardControls in new order
        _cardControls.Clear();
        foreach (var card in newOrder)
        {
            if (lookup.TryGetValue(card, out var ctrl))
                _cardControls.Add(ctrl);
        }

        ShowToast("🌀 Cards Shifted!");
        RepositionCards();
    }

    // ── Game Won ──────────────────────────────────────────────────────────
    private void Engine_GameWon(object? sender, EventArgs e)
    {
        _timer.Stop();
        StopModifierTimers();
        LaunchConfetti();

        var winForm = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score, _difficulty);
        winForm.ShowDialog(this);

        GameFinished?.Invoke(this, _engine.Score);

        if (winForm.PlayAgain) StartGame();
        else Close();
    }

    // ── Timer Events ──────────────────────────────────────────────────────
    private void GameForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _isClosing = true;
        _timer.Stop();
        StopModifierTimers();
    }

    private void Timer_Tick(object? sender, TimerTickEventArgs e)
    {
        _pillTime.SetValue($"{e.Remaining}s");

        int newW = (int)(_timerBarWrap.Width * e.RemainingFraction);
        _timerBarFill.Width = Math.Max(0, newW);
        _timerBarFill.BackColor = e.RemainingFraction < 0.3f ? ColRed : ColTeal;
    }

    private void Timer_TimeUp(object? sender, EventArgs e)
    {
        if (_isClosing) return;

        _pillTime.SetValue("0s");
        ShowToast("⏰ Time's Up!");
        _streakLabel.Text = "Time's up! Better luck next time 🙈";
        StopModifierTimers();

        var winForm = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score,
                                  _difficulty, timeUp: true);
        winForm.ShowDialog(this);

        GameFinished?.Invoke(this, _engine.Score);
        if (winForm.PlayAgain) StartGame();
        else Close();
    }

    // ── UI Helpers ────────────────────────────────────────────────────────
    private void ShowToast(string message)
    {
        _toastLabel.Text    = message;
        _toastLabel.Visible = true;
        _toastLabel.BringToFront();
        _toastTimer.Stop();
        _toastTimer.Start();
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
        int mult = _engine.Score > 0 ? Math.Min(_engine.Streak, 4) : 1;
        _comboLabel.Text      = $"×{mult}";
        _comboLabel.ForeColor = mult >= 4 ? ColRed : mult >= 2 ? ColGold : ColTeal;
    }

    private string BuildStatusText()
    {
        var mods = new List<string>();
        if (_modifiers.HasFlag(GameModifier.ZenMode))         mods.Add("🧘 Zen");
        if (_modifiers.HasFlag(GameModifier.HardcoreMode))    mods.Add("💀 Hardcore");
        if (_modifiers.HasFlag(GameModifier.TripleMatch))     mods.Add("🎲 Triple");
        if (_modifiers.HasFlag(GameModifier.CardDrift))       mods.Add("🌀 Drift");
        if (_modifiers.HasFlag(GameModifier.ShrinkingCards))  mods.Add("📉 Shrink");
        if (_modifiers.HasFlag(GameModifier.FlipLimit))       mods.Add("🔒 FlipLimit");
        if (_modifiers.HasFlag(GameModifier.ComboMultiplier)) mods.Add("⚡ Combo");

        string modsStr = mods.Count > 0 ? "  │  " + string.Join("  ", mods) : "";
        return $"Find the matching monkey pairs! 🐒{modsStr}";
    }

    // ── Confetti ──────────────────────────────────────────────────────────
    private void LaunchConfetti()
    {
        _confetti.Clear();
        Color[] palette = { ColGold, ColRed, ColTeal, Color.White,
                             Color.FromArgb(167, 139, 250) };
        for (int i = 0; i < 180; i++)
            _confetti.Add(new ConfettiParticle(
                _rng.Next(0, Width),
                _rng.Next(-Height, 0),
                _rng.Next(4, 12),
                (float)(_rng.NextDouble() * 3 + 1),
                palette[_rng.Next(palette.Length)],
                (float)(_rng.NextDouble() * 0.15 - 0.075)));

        _confettiPanel.BringToFront();
        _confettiTimer = new System.Windows.Forms.Timer { Interval = 25 };
        _confettiTimer.Tick += (_, _) =>
        {
            bool any = false;
            foreach (var p in _confetti)
            {
                p.Y += p.Speed;
                p.X += (float)Math.Sin(p.Y * p.TiltSpeed) * 2;
                if (p.Y < Height + 20) any = true;
            }
            _confettiPanel.Invalidate();
            if (!any)
            {
                _confettiTimer?.Stop();
                _confettiTimer?.Dispose();
                _confettiTimer = null;
                _confettiPanel.SendToBack();
            }
        };
        _confettiTimer.Start();
    }

    private void PaintConfetti(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        foreach (var p in _confetti)
        {
            using var brush = new SolidBrush(p.Colour);
            g.FillRectangle(brush,
                new RectangleF(p.X, p.Y, p.Size, p.Size * 0.5f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private CardControl? FindControl(CardBase card) =>
        _cardControls.FirstOrDefault(c => ReferenceEquals(c.CardData, card));

    private void PaintTopBar(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var logoFont  = new Font("Segoe UI Black", 18f, FontStyle.Bold, GraphicsUnit.Point);
        using var logoBrush = new SolidBrush(ColGold);
        g.DrawString("🐾 MATSING", logoFont, logoBrush, 14, 22);
    }

    private Button MakeTopButton(string text, EventHandler click)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(100, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(59, 24, 120),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(4, 0, 0, 0),
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += click;
        return btn;
    }

    // ── Dispose ───────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _mismatchTimer.Dispose();
            _toastTimer.Dispose();
            StopModifierTimers();
            _confettiTimer?.Stop();
            _confettiTimer?.Dispose();
            foreach (var c in _cardControls) c.Dispose();
        }
        base.Dispose(disposing);
    }
}

// ── Confetti particle model ───────────────────────────────────────────────────
internal class ConfettiParticle
{
    public float X, Y, Speed, TiltSpeed, Size;
    public Color Colour;
    public ConfettiParticle(float x, float y, float size, float speed, Color colour, float tilt)
    { X=x; Y=y; Size=size; Speed=speed; Colour=colour; TiltSpeed=tilt; }
}