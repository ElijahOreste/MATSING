using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MatchingGame.Models;


namespace MatchingGame.UI
{
    /// <summary>
    /// Custom card button control with animations.
    /// Demonstrates INHERITANCE from Button.
    /// Demonstrates POLYMORPHISM via OnPaint override.
    /// </summary>
    public class CardButton : Button
    {
        private CardBase _card;
        private bool _isAnimating = false;
        private float _flipProgress = 0f;       // 0 = face-down, 1 = face-up
        private Timer _animTimer;
        private bool _isFlippingToReveal = false;

        // Color scheme
        private static readonly Color CardBack = Color.FromArgb(45, 55, 72);
        private static readonly Color CardBackBorder = Color.FromArgb(99, 120, 155);
        private static readonly Color MatchedColor = Color.FromArgb(40, 167, 80);
        private static readonly Color MatchedBorder = Color.FromArgb(72, 220, 110);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CardBase Card
        {
            get => _card;
            set
            {
                _card = value;
                Invalidate();
            }
        }

        public CardButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Emoji", 24f, FontStyle.Regular);

            _animTimer = new Timer { Interval = 16 }; // ~60fps
            _animTimer.Tick += AnimTimer_Tick;
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (_isFlippingToReveal)
                _flipProgress = Math.Min(1f, _flipProgress + 0.12f);
            else
                _flipProgress = Math.Max(0f, _flipProgress - 0.12f);

            Invalidate();

            if (_flipProgress <= 0f || _flipProgress >= 1f)
            {
                _animTimer.Stop();
                _isAnimating = false;
            }
        }

        public void AnimateFlip(bool reveal)
        {
            _isFlippingToReveal = reveal;
            _isAnimating = true;
            _flipProgress = reveal ? 0f : 1f;
            _animTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = new Rectangle(3, 3, Width - 7, Height - 7);
            float radius = 12f;

            if (_card == null)
            {
                DrawCardBack(g, rect, radius);
                return;
            }

            if (_card.IsMatched)
            {
                // Use image drawing for MonkeyCard
                if (_card is MonkeyCard monkeyMatched)
                    DrawMonkeyCard(g, rect, radius, monkeyMatched, matched: true);
                else
                    DrawMatchedCard(g, rect, radius);
                return;
            }

            bool showFront = _card.IsRevealed;

            if (_isAnimating)
            {
                float scaleX = (float)Math.Abs(Math.Cos(Math.PI * _flipProgress));
                var matrix = new System.Drawing.Drawing2D.Matrix();
                float cx = Width / 2f;
                matrix.Translate(cx, 0);
                matrix.Scale(scaleX, 1f);
                matrix.Translate(-cx, 0);
                g.Transform = matrix;

                if (_flipProgress < 0.5f)
                    DrawCardBack(g, rect, radius);
                else
                {
                    if (_card is MonkeyCard monkeyAnim)
                        DrawMonkeyCard(g, rect, radius, monkeyAnim, matched: false);
                    else
                        DrawCardFront(g, rect, radius);
                }

                g.ResetTransform();
            }
            else if (showFront)
            {
                if (_card is MonkeyCard monkey)
                    DrawMonkeyCard(g, rect, radius, monkey, matched: false);
                else
                    DrawCardFront(g, rect, radius);
            }
            else
            {
                DrawCardBack(g, rect, radius);
            }
        }

        private void DrawCardBack(Graphics g, Rectangle rect, float r)
        {
            using (var path = RoundedRect(rect, r))
            {
                using (var brush = new LinearGradientBrush(rect,
                    Color.FromArgb(50, 65, 90), Color.FromArgb(30, 40, 60),
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillPath(brush, path);
                }

                // Pattern dots
                using (var dotBrush = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                {
                    for (int x = 8; x < rect.Width; x += 14)
                        for (int y = 8; y < rect.Height; y += 14)
                            g.FillEllipse(dotBrush, rect.X + x - 2, rect.Y + y - 2, 4, 4);
                }

                // Border
                using (var pen = new Pen(CardBackBorder, 2f))
                    g.DrawPath(pen, path);

                // Question mark
                using (var font = new Font("Segoe UI", 18f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var textBrush = new SolidBrush(Color.FromArgb(100, 150, 200)))
                {
                    g.DrawString("?", font, textBrush, rect, sf);
                }
            }
        }

        private void DrawCardFront(Graphics g, Rectangle rect, float r)
        {
            Color baseColor = _card?.CardColor ?? Color.SteelBlue;

            using (var path = RoundedRect(rect, r))
            {
                var lightColor = Lighten(baseColor, 0.3f);
                using (var brush = new LinearGradientBrush(rect, lightColor, baseColor, LinearGradientMode.Vertical))
                    g.FillPath(brush, path);

                using (var pen = new Pen(Lighten(baseColor, 0.5f), 2.5f))
                    g.DrawPath(pen, path);

                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    string symbol = _card?.GetDisplayText() ?? "";
                    g.DrawString(symbol, Font, Brushes.White, rect, sf);
                }
            }
        }

        private void DrawMonkeyCard(Graphics g, Rectangle rect, float radius, MonkeyCard card, bool matched)
        {
            // Background color
            Color bg = matched
                ? Color.FromArgb(40, 167, 80)
                : Color.FromArgb(50, 30, 15);
            Color border = matched
                ? Color.FromArgb(72, 220, 110)
                : Color.FromArgb(180, 120, 60);

            using (var path = RoundedRect(rect, radius))
            {
                // Fill background
                using (var brush = new LinearGradientBrush(rect,
                    matched ? Color.FromArgb(60, 200, 100) : Color.FromArgb(80, 50, 20),
                    bg, LinearGradientMode.Vertical))
                    g.FillPath(brush, path);

                // Clip to rounded rect so image stays inside
                var prevClip = g.Clip;
                g.SetClip(path);

                // Draw monkey image
                var img = card.GetImage();
                if (img != null)
                {
                    // Scale to fit with small padding
                    int pad = 6;
                    var imgRect = new Rectangle(rect.X + pad, rect.Y + pad,
                                               rect.Width - pad * 2, rect.Height - pad * 2);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(img, imgRect);
                }
                else
                {
                    // Fallback: draw emoji text
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString("🐒", Font, Brushes.White, rect, sf);
                }

                g.Clip = prevClip;

                // Border
                using (var pen = new Pen(border, 2.5f))
                    g.DrawPath(pen, path);

                // Matched: subtle glowing overlay
                if (matched)
                {
                    using (var glow = new SolidBrush(Color.FromArgb(40, 100, 255, 100)))
                        g.FillPath(glow, path);
                }
            }
        }

        private void DrawMatchedCard(Graphics g, Rectangle rect, float r)
        {
            using (var path = RoundedRect(rect, r))
            {
                using (var brush = new LinearGradientBrush(rect,
                    Color.FromArgb(60, 200, 100), MatchedColor, LinearGradientMode.Vertical))
                    g.FillPath(brush, path);

                using (var pen = new Pen(MatchedBorder, 2.5f))
                    g.DrawPath(pen, path);

                // Symbol with slight glow
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    string symbol = _card?.GetDisplayText() ?? "";
                    // Draw shadow
                    var shadowRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                        g.DrawString(symbol, Font, shadowBrush, shadowRect, sf);
                    g.DrawString(symbol, Font, Brushes.White, rect, sf);
                }
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Color Lighten(Color color, float amount)
        {
            int r = (int)Math.Min(255, color.R + (255 - color.R) * amount);
            int g = (int)Math.Min(255, color.G + (255 - color.G) * amount);
            int b = (int)Math.Min(255, color.B + (255 - color.B) * amount);
            return Color.FromArgb(r, g, b);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (_card != null && !_card.IsRevealed && !_card.IsMatched)
                BackColor = Color.FromArgb(60, 80, 110);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = Color.Transparent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _animTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
