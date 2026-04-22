using MATSING.Models;

namespace MATSING.Forms;

/// <summary>
/// Victory / Time-Up result dialog shown at the end of each game.
/// Displays final stats and lets the player choose to play again or quit.
/// </summary>
public class WinForm : Form
{
    private static readonly Color ColBg    = Color.FromArgb( 43,  16,  80);
    private static readonly Color ColGold  = Color.FromArgb(247, 201,  72);
    private static readonly Color ColTeal  = Color.FromArgb( 94, 255, 209);
    private static readonly Color ColRed   = Color.FromArgb(255, 107, 107);
    private static readonly Color ColWhite = Color.White;

    /// <summary>True if the player clicked "Play Again".</summary>
    public bool PlayAgain { get; private set; }

    private readonly int        _moves;
    private readonly int        _timeSeconds;
    private readonly int        _score;
    private readonly Difficulty _difficulty;
    private readonly bool       _timeUp;

    // ── Constructor ───────────────────────────────────────────────────────
    public WinForm(int moves, int timeSeconds, int score,
                   Difficulty difficulty, bool timeUp = false)
    {
        _moves       = moves;
        _timeSeconds = timeSeconds;
        _score       = score;
        _difficulty  = difficulty;
        _timeUp      = timeUp;

        InitialiseForm();
        BuildUI();
    }

    private void InitialiseForm()
    {
        Text            = _timeUp ? "MATSING – Time's Up!" : "MATSING – You Win! 🎉";
        Size            = new Size(500, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = ColBg;
        DoubleBuffered  = true;
        MaximizeBox     = false;
        MinimizeBox     = false;
    }

    private void BuildUI()
    {
        // Header panel
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 150,
            BackColor = Color.Transparent,
        };
        header.Paint += PaintHeader;

        // Stats panel
        var statsPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 130,
            BackColor = Color.FromArgb(30, 255, 255, 255),
            Padding   = new Padding(30, 10, 30, 10),
        };
        statsPanel.Paint += PaintStats;

        // Buttons panel
        var btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 90,
            BackColor = Color.Transparent,
        };

        var playAgainBtn = MakeButton("▶  Play Again", ColGold, Color.FromArgb(26, 10, 46));
        playAgainBtn.Click += (_, _) => { PlayAgain = true;  Close(); };

        var menuBtn = MakeButton("⌂  Main Menu", Color.FromArgb(59, 24, 120), ColWhite);
        menuBtn.Click += (_, _) => { PlayAgain = false; Close(); };

        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
        };
        flow.Controls.AddRange(new Control[] { playAgainBtn, menuBtn });
        btnPanel.Controls.Add(flow);
        btnPanel.Resize += (_, _) =>
            flow.Location = new Point(
                (btnPanel.Width  - flow.PreferredSize.Width)  / 2,
                (btnPanel.Height - flow.PreferredSize.Height) / 2);

        Controls.Add(btnPanel);
        Controls.Add(statsPanel);
        Controls.Add(header);
    }

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var bg = new SolidBrush(Color.FromArgb(26, 10, 46));
        g.FillRectangle(bg, p.ClientRectangle);

        string emoji  = _timeUp ? "⏰" : "🎉";
        string title  = _timeUp ? "TIME'S UP!" : "YOU WIN!";
        string sub    = _timeUp
            ? "Better luck next time! Keep practicing 🙈"
            : $"All monkey pairs matched on {_difficulty}! 🐒";

        using var emojiFont = new Font("Segoe UI Emoji", 36f, FontStyle.Regular, GraphicsUnit.Point);
        using var emojiB    = new SolidBrush(ColWhite);
        var eSize = g.MeasureString(emoji, emojiFont);
        g.DrawString(emoji, emojiFont, emojiB, (p.Width - eSize.Width) / 2f, 12f);

        using var titleFont  = new Font("Segoe UI Black", 28f, FontStyle.Bold, GraphicsUnit.Point);
        using var titleBrush = new SolidBrush(_timeUp ? ColRed : ColGold);
        var tSize = g.MeasureString(title, titleFont);
        g.DrawString(title, titleFont, titleBrush, (p.Width - tSize.Width) / 2f, 56f);

        using var subFont  = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var subBrush = new SolidBrush(ColTeal);
        var sSize = g.MeasureString(sub, subFont);
        g.DrawString(sub, subFont, subBrush, (p.Width - sSize.Width) / 2f, 110f);
    }

    private void PaintStats(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var stats = new (string Label, string Value)[]
        {
            ("MOVES",      _moves.ToString()),
            ("TIME",       $"{_timeSeconds}s"),
            ("SCORE",      _score.ToString()),
            ("DIFFICULTY", _difficulty.ToString().ToUpper()),
        };

        using var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold, GraphicsUnit.Point);
        using var valFont   = new Font("Segoe UI Black", 18f, FontStyle.Bold, GraphicsUnit.Point);
        using var labelB    = new SolidBrush(ColTeal);
        using var valB      = new SolidBrush(ColGold);
        using var divPen    = new Pen(Color.FromArgb(50, 255, 255, 255), 1f);

        float colW = p.Width / 4f;
        for (int i = 0; i < stats.Length; i++)
        {
            float x = i * colW;
            // Divider
            if (i > 0) g.DrawLine(divPen, x, 10, x, p.Height - 10);

            var lSize = g.MeasureString(stats[i].Label, labelFont);
            var vSize = g.MeasureString(stats[i].Value, valFont);

            g.DrawString(stats[i].Label, labelFont, labelB,
                x + (colW - lSize.Width) / 2f, 18f);
            g.DrawString(stats[i].Value, valFont, valB,
                x + (colW - vSize.Width) / 2f, 42f);
        }
    }

    private static Button MakeButton(string text, Color bg, Color fg)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(180, 52),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Font      = new Font("Segoe UI Black", 12f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(8, 0, 8, 0),
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = ControlPaint.Light(bg, 0.15f);
        btn.MouseLeave += (_, _) => btn.BackColor = bg;
        return btn;
    }
}
