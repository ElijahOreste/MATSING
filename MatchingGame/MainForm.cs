using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MatchingGame.Controllers;
using MatchingGame.Interfaces;
using MatchingGame.Models;
using MatchingGame.UI;
using MatchingGame.Utils;

namespace MatchingGame
{
    /// <summary>
    /// Main game form with modern dark UI.
    /// Demonstrates all OOP principles in orchestration.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly GameController _controller;
        private CardButton[,] _cardButtons;
        private Timer _hideTimer;
        private Timer _countdownTimer;
        private Timer _comboTimer;
        private int _timeRemaining;
        private int _comboCount = 0;
        private CardButton _lastMismatch1, _lastMismatch2;

        // UI Controls
        private Panel _headerPanel;
        private Panel _boardPanel;
        private Panel _footerPanel;
        private Label _titleLabel;
        private Label _scoreLabel;
        private Label _movesLabel;
        private Label _timerLabel;
        private Label _matchesLabel;
        private Label _comboLabel;
        private Button _newGameBtn;
        private Button _easyBtn, _mediumBtn, _hardBtn, _monkeyBtn;
        private Difficulty _selectedDifficulty = Difficulty.Easy;

        // Colors
        private static readonly Color BgDark = Color.FromArgb(15, 20, 35);
        private static readonly Color BgCard = Color.FromArgb(22, 30, 50);
        private static readonly Color AccentBlue = Color.FromArgb(85, 183, 255);
        private static readonly Color AccentGold = Color.FromArgb(255, 215, 0);
        private static readonly Color AccentGreen = Color.FromArgb(78, 220, 130);
        private static readonly Color TextPrimary = Color.FromArgb(230, 240, 255);
        private static readonly Color TextSecondary = Color.FromArgb(130, 150, 180);

        public MainForm()
        {
            // Pre-load monkey images from SVGs embedded PNG data
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string monkeyFolder = Path.Combine(appDir, "MonkeyImages");
            MonkeyImageLoader.Initialize(monkeyFolder);

            _controller = new GameController();
            _controller.MatchFound += OnMatchFound;
            _controller.MatchMissed += OnMatchMissed;
            _controller.GameCompleted += OnGameCompleted;

            _hideTimer = new Timer { Interval = 900 };
            _hideTimer.Tick += HideTimer_Tick;

            _countdownTimer = new Timer { Interval = 1000 };
            _countdownTimer.Tick += CountdownTimer_Tick;

            _comboTimer = new Timer { Interval = 1500 };
            _comboTimer.Tick += (s, e) => { _comboTimer.Stop(); _comboLabel.Visible = false; };

            InitializeUI();
            StartNewGame();
        }

        private void InitializeUI()
        {
            this.Text = "✨ Memory Match - Matching Game";
            this.Size = new Size(820, 700);
            this.MinimumSize = new Size(700, 600);
            this.BackColor = BgDark;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 10f);

            // Header
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(18, 25, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            _headerPanel.Paint += HeaderPanel_Paint;

            _titleLabel = new Label
            {
                Text = "✨ MEMORY MATCH",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Location = new Point(20, 18)
            };

            _scoreLabel = MakeStatLabel("SCORE\n0", 280, 10);
            _movesLabel = MakeStatLabel("MOVES\n0", 400, 10);
            _matchesLabel = MakeStatLabel("PAIRS\n0/0", 510, 10);
            _timerLabel = new Label
            {
                Text = "∞",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = AccentGold,
                Location = new Point(640, 28),
                AutoSize = true
            };

            _comboLabel = new Label
            {
                Text = "🔥 COMBO x2!",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = AccentGold,
                Location = new Point(620, 12),
                AutoSize = true,
                Visible = false
            };

            _headerPanel.Controls.AddRange(new Control[]
            {
                _titleLabel, _scoreLabel, _movesLabel, _matchesLabel, _timerLabel, _comboLabel
            });

            // Footer / controls
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(18, 25, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            _footerPanel.Paint += FooterPanel_Paint;

            _easyBtn   = MakeDifficultyButton("Easy 4×4",     Difficulty.Easy,   20);
            _mediumBtn  = MakeDifficultyButton("Medium 6×6",   Difficulty.Medium, 130);
            _hardBtn    = MakeDifficultyButton("Hard ⏱ 6×6",  Difficulty.Hard,   270);
            _monkeyBtn  = MakeDifficultyButton("🐒 Monkeys",   Difficulty.Monkey, 390);

            _newGameBtn = new Button
            {
                Text = "▶  New Game",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = BgDark,
                BackColor = AccentGreen,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 42),
                Location = new Point(660, 14),
                Cursor = Cursors.Hand
            };
            _newGameBtn.FlatAppearance.BorderSize = 0;
            _newGameBtn.Click += (s, e) => StartNewGame();

            _footerPanel.Controls.AddRange(new Control[]
            {
                _easyBtn, _mediumBtn, _hardBtn, _monkeyBtn, _newGameBtn
            });

            // Board panel
            _boardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(20)
            };

            this.Controls.Add(_boardPanel);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_footerPanel);
        }

        private Label MakeStatLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextSecondary,
                Location = new Point(x, y),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private Button MakeDifficultyButton(string text, Difficulty diff, int x)
        {
            bool isSelected = diff == _selectedDifficulty;
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = isSelected ? BgDark : TextSecondary,
                BackColor = isSelected ? AccentBlue : Color.FromArgb(35, 45, 70),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 42),
                Location = new Point(x, 14),
                Cursor = Cursors.Hand,
                Tag = diff
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = isSelected ? AccentBlue : Color.FromArgb(60, 80, 120);
            btn.Click += DifficultyButton_Click;
            return btn;
        }

        private void DifficultyButton_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            _selectedDifficulty = (Difficulty)btn.Tag;
            UpdateDifficultyButtons();
        }

        private void UpdateDifficultyButtons()
        {
            foreach (var btn in new[] { _easyBtn, _mediumBtn, _hardBtn, _monkeyBtn })
            {
                var diff = (Difficulty)btn.Tag;
                bool selected = diff == _selectedDifficulty;
                btn.ForeColor = selected ? BgDark : TextSecondary;
                btn.BackColor = selected ? (diff == Difficulty.Monkey ? Color.FromArgb(200, 120, 40) : AccentBlue)
                                         : Color.FromArgb(35, 45, 70);
                btn.FlatAppearance.BorderColor = selected
                    ? (diff == Difficulty.Monkey ? Color.FromArgb(200, 120, 40) : AccentBlue)
                    : Color.FromArgb(60, 80, 120);
            }

            // Update title to reflect monkey mode
            _titleLabel.Text = _selectedDifficulty == Difficulty.Monkey
                ? "🐒 MONKEY MATCH"
                : "✨ MEMORY MATCH";
            _titleLabel.ForeColor = _selectedDifficulty == Difficulty.Monkey
                ? Color.FromArgb(200, 120, 40)
                : AccentBlue;
        }

        private void StartNewGame()
        {
            _countdownTimer.Stop();
            _hideTimer.Stop();
            _comboCount = 0;
            _comboLabel.Visible = false;

            _controller.StartNewGame(_selectedDifficulty);
            BuildBoard();
            UpdateStatsUI();

            if (_controller.Settings.TimeLimitSeconds > 0)
            {
                _timeRemaining = _controller.Settings.TimeLimitSeconds;
                _timerLabel.ForeColor = AccentGold;
                UpdateTimerLabel();
                _countdownTimer.Start();
            }
            else
            {
                _timerLabel.Text = "∞";
                _timerLabel.ForeColor = AccentGold;
            }
        }

        private void BuildBoard()
        {
            _boardPanel.Controls.Clear();

            int size = _controller.Settings.GridSize;
            int totalPairs = (size * size) / 2;
            _matchesLabel.Text = $"PAIRS\n0/{totalPairs}";

            int padding = 20;
            int spacing = 8;
            int boardW = _boardPanel.Width - padding * 2;
            int boardH = _boardPanel.Height - padding * 2;
            int cardW = (boardW - spacing * (size - 1)) / size;
            int cardH = (boardH - spacing * (size - 1)) / size;
            cardW = Math.Max(cardW, 55);
            cardH = Math.Max(cardH, 55);

            _cardButtons = new CardButton[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var cardBtn = new CardButton
                    {
                        Card = _controller.Board[r][c],
                        Size = new Size(cardW, cardH),
                        Location = new Point(padding + c * (cardW + spacing), padding + r * (cardH + spacing)),
                        Tag = (r, c),
                        BackColor = Color.Transparent
                    };
                    cardBtn.Click += CardButton_Click;
                    _boardPanel.Controls.Add(cardBtn);
                    _cardButtons[r, c] = cardBtn;
                }
            }
        }

        private void CardButton_Click(object sender, EventArgs e)
        {
            if (_hideTimer.Enabled) return;

            var btn = (CardButton)sender;
            var (row, col) = ((int, int))btn.Tag;

            bool moved = _controller.RevealCard(row, col);
            if (moved)
            {
                btn.AnimateFlip(true);
                btn.Invalidate();
                UpdateStatsUI();
            }
        }

        private void OnMatchFound(object sender, MatchEventArgs e)
        {
            _comboCount++;
            UpdateCardDisplay(e.Card1.Row, e.Card1.Column);
            UpdateCardDisplay(e.Card2.Row, e.Card2.Column);

            if (_comboCount >= 2)
            {
                _comboLabel.Text = $"🔥 COMBO x{_comboCount}!";
                _comboLabel.Visible = true;
                _comboTimer.Stop();
                _comboTimer.Start();
            }

            UpdateStatsUI();
        }

        private void OnMatchMissed(object sender, MatchEventArgs e)
        {
            _comboCount = 0;
            _comboLabel.Visible = false;
            _lastMismatch1 = _cardButtons[e.Card1.Row, e.Card1.Column];
            _lastMismatch2 = _cardButtons[e.Card2.Row, e.Card2.Column];

            // Flash red briefly
            FlashCard(_lastMismatch1, Color.FromArgb(200, 60, 60));
            FlashCard(_lastMismatch2, Color.FromArgb(200, 60, 60));

            _hideTimer.Start();
        }

        private void FlashCard(CardButton btn, Color color)
        {
            // Brief visual cue handled by card repaint
            btn.Invalidate();
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            _controller.HideMismatchedCards();
            _lastMismatch1?.AnimateFlip(false);
            _lastMismatch2?.AnimateFlip(false);
            _lastMismatch1?.Invalidate();
            _lastMismatch2?.Invalidate();
            UpdateStatsUI();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _timeRemaining--;
            UpdateTimerLabel();

            if (_timeRemaining <= 10)
                _timerLabel.ForeColor = Color.FromArgb(255, 80, 80);

            if (_timeRemaining <= 0)
            {
                _countdownTimer.Stop();
                ShowGameOver(false);
            }
        }

        private void UpdateTimerLabel()
        {
            int m = _timeRemaining / 60;
            int s = _timeRemaining % 60;
            _timerLabel.Text = $"{m}:{s:D2}";
        }

        private void OnGameCompleted(object sender, EventArgs e)
        {
            _countdownTimer.Stop();
            this.BeginInvoke((Action)(() => ShowGameOver(true)));
        }

        private void ShowGameOver(bool won)
        {
            var result = new GameResultForm(won, _controller.State, _controller.Settings);
            result.ShowDialog(this);
            if (result.PlayAgain)
                StartNewGame();
        }

        private void UpdateCardDisplay(int row, int col)
        {
            _cardButtons[row, col]?.Invalidate();
        }

        private void UpdateStatsUI()
        {
            if (_controller.State == null) return;
            int totalPairs = _controller.State.TotalPairs;
            _scoreLabel.Text = $"SCORE\n{_controller.State.Score}";
            _movesLabel.Text = $"MOVES\n{_controller.State.Moves}";
            _matchesLabel.Text = $"PAIRS\n{_controller.State.MatchesFound}/{totalPairs}";
        }

        private void HeaderPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = new Rectangle(0, _headerPanel.Height - 1, _headerPanel.Width, 1);
            using (var pen = new Pen(Color.FromArgb(40, 60, 100), 1))
                g.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
        }

        private void FooterPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var pen = new Pen(Color.FromArgb(40, 60, 100), 1))
                g.DrawLine(pen, 0, 0, _footerPanel.Width, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_cardButtons != null)
                BuildBoard();
        }
    }
}
