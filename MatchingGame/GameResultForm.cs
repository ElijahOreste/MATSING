using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MatchingGame.Models;

namespace MatchingGame
{
    /// <summary>
    /// Game over / victory screen.
    /// Demonstrates Encapsulation and clean separation of UI concerns.
    /// </summary>
    public class GameResultForm : Form
    {
        public bool PlayAgain { get; private set; } = false;
        private Timer _confettiTimer;
        private Random _rng = new Random();
        private ConfettiParticle[] _particles;

        private static readonly Color BgDark = Color.FromArgb(15, 20, 35);
        private static readonly Color AccentBlue = Color.FromArgb(85, 183, 255);
        private static readonly Color AccentGold = Color.FromArgb(255, 215, 0);
        private static readonly Color AccentGreen = Color.FromArgb(78, 220, 130);

        public GameResultForm(bool won, GameState state, DifficultySettings settings)
        {
            this.Text = won ? "🎉 You Won!" : "⏰ Time's Up!";
            this.Size = new Size(460, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BgDark;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.DoubleBuffered = true;

            BuildUI(won, state, settings);

            if (won)
            {
                InitConfetti();
                _confettiTimer = new Timer { Interval = 30 };
                _confettiTimer.Tick += (s, e) => Invalidate();
                _confettiTimer.Start();
            }
        }

        private void BuildUI(bool won, GameState state, DifficultySettings settings)
        {
            // Emoji trophy or clock
            var emojiLabel = new Label
            {
                Text = won ? "🏆" : "⏰",
                Font = new Font("Segoe UI Emoji", 48f),
                ForeColor = won ? AccentGold : Color.OrangeRed,
                AutoSize = true,
                Location = new Point(180, 25)
            };

            var titleLabel = new Label
            {
                Text = won ? "CONGRATULATIONS!" : "GAME OVER",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = won ? AccentGold : Color.OrangeRed,
                AutoSize = true,
                Location = new Point(0, 115),
                Width = 460,
                TextAlign = ContentAlignment.MiddleCenter
            };

            int finalScore = state.CalculateFinalScore();
            TimeSpan elapsed = state.ElapsedTime;

            var statsLabel = new Label
            {
                Text = $"Difficulty: {settings.DisplayName}\n" +
                       $"Final Score: {finalScore}\n" +
                       $"Moves: {state.Moves}\n" +
                       $"Pairs Found: {state.MatchesFound}/{state.TotalPairs}\n" +
                       $"Time: {(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(180, 200, 230),
                AutoSize = false,
                Size = new Size(260, 130),
                Location = new Point(100, 155),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var playAgainBtn = MakeButton("▶  Play Again", AccentGreen, 90, 305);
            playAgainBtn.Click += (s, e) => { PlayAgain = true; this.Close(); };

            var quitBtn = MakeButton("✕  Quit", Color.FromArgb(60, 70, 100), 260, 305);
            quitBtn.Click += (s, e) => { PlayAgain = false; this.Close(); };

            this.Controls.AddRange(new Control[]
            {
                emojiLabel, titleLabel, statsLabel, playAgainBtn, quitBtn
            });
        }

        private Button MakeButton(string text, Color bg, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 20, 35),
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 44),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void InitConfetti()
        {
            _particles = new ConfettiParticle[60];
            Color[] colors = { AccentGold, AccentGreen, AccentBlue,
                               Color.HotPink, Color.Orange, Color.LimeGreen };
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new ConfettiParticle
                {
                    X = _rng.Next(Width),
                    Y = _rng.Next(-50, -5),
                    SpeedX = (float)(_rng.NextDouble() * 4 - 2),
                    SpeedY = (float)(_rng.NextDouble() * 4 + 2),
                    Color = colors[_rng.Next(colors.Length)],
                    Size = _rng.Next(6, 14),
                    Rotation = _rng.Next(360)
                };
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_particles == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var p in _particles)
            {
                p.X += p.SpeedX;
                p.Y += p.SpeedY;
                p.Rotation += 5;
                if (p.Y > Height + 10) p.Y = -10;

                var state = g.Save();
                g.TranslateTransform(p.X, p.Y);
                g.RotateTransform(p.Rotation);
                using (var brush = new SolidBrush(p.Color))
                    g.FillRectangle(brush, -p.Size / 2f, -p.Size / 4f, p.Size, p.Size / 2f);
                g.Restore(state);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _confettiTimer?.Stop();
                _confettiTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class ConfettiParticle
        {
            public float X, Y, SpeedX, SpeedY, Rotation;
            public Color Color;
            public int Size;
        }
    }
}
