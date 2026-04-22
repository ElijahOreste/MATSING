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
    private readonly GameEngine _engine = new();
    private readonly GameTimer  _timer  = new();
    private readonly Difficulty _difficulty;

    // ═══════════════════════════════════════════════════════════════
    //  POLYMORPHISM IN ACTION
    //  This list holds CardControl references — at runtime some are
    //  MonkeyCardControl, some SpecialCardControl.
    //  Calling PlayFlipAnimation() / PlayMatchAnimation() on any
    //  element dispatches to the correct subclass override.
    // ═══════════════════════════════════════════════════════════════
    private readonly List<CardControl> _cardControls = new();

    // ── UI Controls ───────────────────────────────────────────────────────
    private Panel              _topBar       = null!;
    private Panel              _timerBarWrap = null!;
    private Panel              _timerBarFill = null!;
    private Panel              _boardPanel   = null!;
    private Panel              _bottomBar    = null!;
    private Controls.ScorePillControl _pillMatches = null!, _pillMoves = null!,
                                       _pillScore  = null!, _pillTime  = null!;
    private Label              _streakLabel  = null!;
    private Label              _toastLabel   = null!;
    private System.Windows.Forms.Timer _toastTimer  = null!;
    private System.Windows.Forms.Timer _mismatchTimer = null!;

    // ── Confetti ──────────────────────────────────────────────────────────
    private List<ConfettiParticle> _confetti = new();
    private System.Windows.Forms.Timer _confettiTimer = null!;
    private Panel _confettiPanel = null!;
    private readonly Random _rng = new();

    // ── Event ─────────────────────────────────────────────────────────────
    /// <summary>Raised when the game ends (win or time-up). Carries final score.</summary>
    public event EventHandler<int>? GameFinished;

    // ── Constructor ───────────────────────────────────────────────────────
    public GameForm(Difficulty difficulty)
    {
        _difficulty = difficulty;
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
        _pillTime    = new Controls.ScorePillControl("Time",    "—");

        var pillFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0),
        };
        pillFlow.Controls.AddRange(new Control[]
            { _pillMatches, _pillMoves, _pillScore, _pillTime });

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
            Text      = "Find the matching monkey pairs! 🐒",
        };
        _bottomBar.Controls.Add(_streakLabel);

        // ── Board panel ───────────────────────────────────────────────────
        _boardPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll= true,
        };

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
        _engine.MatchFound    += Engine_MatchFound;
        _engine.MismatchFound += Engine_MismatchFound;
        _engine.GameWon       += Engine_GameWon;
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
        _timer.Reset();
        _engine.StartNewGame(_difficulty);
        BuildCardGrid();
        UpdatePills();
        _streakLabel.Text = "Find the matching monkey pairs! 🐒";
        _timerBarFill.BackColor = ColTeal;
        _timer.Start(_engine.Config.TimeLimitS);
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

        int cols    = cfg.Columns;
        int cardW   = cfg.CardWidth;
        int cardH   = cfg.CardHeight;
        int gapX    = 14, gapY = 14;
        int rows    = (int)Math.Ceiling((double)deck.Count / cols);
        int gridW   = cols * cardW + (cols - 1) * gapX;
        int gridH   = rows * cardH + (rows - 1) * gapY;

        for (int i = 0; i < deck.Count; i++)
        {
            var card = deck[i];

            // ═══════════════════════════════════════════════════════════════
            //  POLYMORPHISM IN ACTION
            //  Factory: creates the correct CardControl subtype per card model.
            //  After this point the list holds CardControl references — the
            //  concrete type is invisible to the rest of GameForm.
            // ═══════════════════════════════════════════════════════════════
            CardControl ctrl = card switch
            {
                MonkeyCard  m => new MonkeyCardControl(m),
                SpecialCard s => new SpecialCardControl(s),
                _             => new CardControl(card),
            };

            ctrl.Size   = new Size(cardW, cardH);
            int col = i % cols, row = i / cols;

            // Centre the grid within the board panel
            _boardPanel.Resize += (_, _) => RepositionCards(gridW, gridH, cardW, cardH, gapX, gapY, cols);

            ctrl.Left = col * (cardW + gapX);
            ctrl.Top  = row * (cardH + gapY);
            ctrl.CardClicked += Card_Clicked;

            _cardControls.Add(ctrl);
            _boardPanel.Controls.Add(ctrl);
        }

        RepositionCards(gridW, gridH, cardW, cardH, gapX, gapY, cols);
        _toastLabel.BringToFront();
    }

    private void RepositionCards(int gridW, int gridH, int cardW, int cardH, int gapX, int gapY, int cols)
    {
        int offX = Math.Max(10, (_boardPanel.Width  - gridW) / 2);
        int offY = Math.Max(10, (_boardPanel.Height - gridH) / 2);
        for (int i = 0; i < _cardControls.Count; i++)
        {
            int col = i % cols, row = i / cols;
            _cardControls[i].Left = offX + col * (cardW + gapX);
            _cardControls[i].Top  = offY + row * (cardH + gapY);
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

        // ─────────────────────────────────────────────────────────────────
        //  POLYMORPHISM: ctrl might be MonkeyCardControl OR SpecialCardControl.
        //  We call the same method — CLR dispatches to the right override.
        // ─────────────────────────────────────────────────────────────────
        ctrl.PlayFlipAnimation();
    }

    // ── Match Found ───────────────────────────────────────────────────────
    private void Engine_MatchFound(object? sender, CardPairEventArgs e)
    {
        var a = FindControl(e.First);
        var b = FindControl(e.Second);

        // Polymorphic match animation
        a?.PlayMatchAnimation();
        b?.PlayMatchAnimation();

        int streak = _engine.Streak;
        if (streak >= 2)
        {
            ShowToast($"🔥 {streak}x Combo!");
            _streakLabel.Text = $"🔥 {streak}x Combo Streak!";
        }
        else
        {
            ShowToast("🎯 Match!");
            _streakLabel.Text = $"Matched: {_engine.MatchCount} / {_engine.Config.PairCount}";
        }

        UpdatePills();
    }

    // ── Mismatch Found ────────────────────────────────────────────────────
    private void Engine_MismatchFound(object? sender, CardPairEventArgs e)
    {
        UpdatePills();
        _streakLabel.Text = "Not a match — keep looking! 🙈";
        _mismatchTimer.Start();
    }

    private void MismatchTimer_Tick(object? sender, EventArgs e)
    {
        _mismatchTimer.Stop();
        _engine.FlipDownMismatch();

        // Flip the two face-up non-matched controls back
        // Refresh all non-matched controls
        foreach (var ctrl in _cardControls)
            if (!ctrl.CardData.IsMatched) ctrl.Invalidate();
    }

    // ── Game Won ──────────────────────────────────────────────────────────
    private void Engine_GameWon(object? sender, EventArgs e)
    {
        _timer.Stop();
        LaunchConfetti();

        var winForm = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score, _difficulty);
        winForm.ShowDialog(this);

        GameFinished?.Invoke(this, _engine.Score);

        if (winForm.PlayAgain) StartGame();
        else Close();
    }

    // ── Timer Events ──────────────────────────────────────────────────────
    private void Timer_Tick(object? sender, TimerTickEventArgs e)
    {
        _pillTime.SetValue($"{e.Remaining}s");

        // Update progress bar
        int newW = (int)(_timerBarWrap.Width * e.RemainingFraction);
        _timerBarFill.Width = Math.Max(0, newW);

        // Warn when low
        _timerBarFill.BackColor = e.RemainingFraction < 0.3f ? ColRed : ColTeal;
    }

    private void Timer_TimeUp(object? sender, EventArgs e)
    {
        _pillTime.SetValue("0s");
        ShowToast("⏰ Time's Up!");
        _streakLabel.Text = "Time's up! Better luck next time 🙈";

        var winForm = new WinForm(_engine.MoveCount, _timer.Elapsed, _engine.Score,
                                  _difficulty, timeUp: true);
        winForm.ShowDialog(this);

        GameFinished?.Invoke(this, _engine.Score);
        if (winForm.PlayAgain) StartGame();
        else Close();
    }

    // ── Toast ─────────────────────────────────────────────────────────────
    private void ShowToast(string message)
    {
        _toastLabel.Text    = message;
        _toastLabel.Visible = true;
        _toastLabel.BringToFront();
        _toastTimer.Stop();
        _toastTimer.Start();
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
                _confettiTimer.Stop();
                _confettiTimer.Dispose();
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

    private void UpdatePills()
    {
        _pillMatches.SetValue(_engine.MatchCount.ToString());
        _pillMoves  .SetValue(_engine.MoveCount .ToString());
        _pillScore  .SetValue(_engine.Score     .ToString());
    }

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
