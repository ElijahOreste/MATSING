namespace MATSING.Controls;

/// <summary>
/// <b>INHERITANCE</b> — ScorePillControl extends <see cref="Panel"/><br/>
/// A reusable pill-shaped score display used in the game header.
/// Shows a teal label (e.g. "MATCHES") above a large gold value.
/// </summary>
public class ScorePillControl : Panel
{
    private string _label;
    private string _value;

    private static readonly Color ColBg    = Color.FromArgb(43,  16,  80);
    private static readonly Color ColBorder= Color.FromArgb(59,  24, 120);
    private static readonly Color ColGold  = Color.FromArgb(247, 201,  72);
    private static readonly Color ColTeal  = Color.FromArgb( 94, 255, 209);

    /// <summary>Creates a ScorePillControl with a label and initial value.</summary>
    public ScorePillControl(string label, string initialValue = "0")
    {
        _label        = label;
        _value        = initialValue;
        DoubleBuffered = true;
        Size          = new Size(110, 64);
        Cursor        = Cursors.Default;
    }

    /// <summary>Updates the displayed value and repaints.</summary>
    public void SetValue(string value)
    {
        _value = value;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(1, 1, Width - 2, Height - 2);

        // Background pill
        using var bgBrush = new SolidBrush(ColBg);
        using var path    = RoundedPath(rect, Height / 2);
        g.FillPath(bgBrush, path);

        // Border
        using var pen = new Pen(ColBorder, 2f);
        g.DrawPath(pen, path);

        // Label (small, teal, uppercase)
        using var labelFont = new Font("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(ColTeal);
        var labelStr  = _label.ToUpper();
        var labelSize = g.MeasureString(labelStr, labelFont);
        g.DrawString(labelStr, labelFont, labelBrush,
            (Width - labelSize.Width) / 2f, 8f);

        // Value (large, gold, bold)
        using var valueFont = new Font("Segoe UI Black", 18f, FontStyle.Bold, GraphicsUnit.Point);
        using var valueBrush = new SolidBrush(ColGold);
        var valSize = g.MeasureString(_value, valueFont);
        g.DrawString(_value, valueFont, valueBrush,
            (Width - valSize.Width) / 2f,
            Height - valSize.Height - 6f);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X,         r.Y,          d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}
