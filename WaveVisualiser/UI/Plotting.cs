using System.Drawing.Drawing2D;

namespace WaveVisualiser.UI;

/// <summary>
/// Small helpers for drawing an x-y plot: axes with ticks and labels, and
/// curves sampled from a function. Everything is in data coordinates and
/// mapped to pixels through <see cref="Axes"/>.
/// </summary>
public sealed class Axes
{
    public Axes(Rectangle area, double xMin, double xMax, double yMin, double yMax)
    {
        Area = area;
        XMin = xMin; XMax = xMax; YMin = yMin; YMax = yMax;
    }

    public Rectangle Area { get; }
    public double XMin { get; }
    public double XMax { get; }
    public double YMin { get; }
    public double YMax { get; }

    public float ToPixelX(double x) => (float)(Area.Left + (x - XMin) / (XMax - XMin) * Area.Width);
    public float ToPixelY(double y) => (float)(Area.Bottom - (y - YMin) / (YMax - YMin) * Area.Height);
    public PointF ToPixel(double x, double y) => new(ToPixelX(x), ToPixelY(y));
}

public static class Plotting
{
    public static readonly Color AxisColour = Color.FromArgb(90, 90, 90);
    public static readonly Color GridColour = Color.FromArgb(225, 225, 225);
    public static readonly Color Wave1Colour = Color.FromArgb(31, 119, 180);
    public static readonly Color Wave2Colour = Color.FromArgb(44, 160, 44);
    public static readonly Color SumColour = Color.FromArgb(20, 20, 20);
    public static readonly Color EnvelopeColour = Color.FromArgb(214, 39, 40);
    public static readonly Color NodeColour = Color.FromArgb(214, 39, 40);

    private static readonly Font LabelFont = new("Segoe UI", 9f);
    private static readonly Font TitleFont = new("Segoe UI", 10f, FontStyle.Bold);

    /// <summary>Leave room for axis labels and return the plotting area.</summary>
    public static Rectangle Inset(Rectangle bounds) =>
        Rectangle.FromLTRB(bounds.Left + 52, bounds.Top + 28, bounds.Right - 16, bounds.Bottom - 34);

    public static void DrawAxes(Graphics g, Axes ax, string xLabel, string yLabel, string? title = null,
                                double xTick = 0, double yTick = 0)
    {
        using var gridPen = new Pen(GridColour);
        using var axisPen = new Pen(AxisColour);
        using var textBrush = new SolidBrush(AxisColour);

        if (xTick > 0)
        {
            for (double x = Math.Ceiling(ax.XMin / xTick) * xTick; x <= ax.XMax + 1e-9; x += xTick)
            {
                float px = ax.ToPixelX(x);
                g.DrawLine(gridPen, px, ax.Area.Top, px, ax.Area.Bottom);
                string label = Math.Abs(x) < 1e-9 ? "0" : x.ToString("0.##");
                var size = g.MeasureString(label, LabelFont);
                g.DrawString(label, LabelFont, textBrush, px - size.Width / 2, ax.Area.Bottom + 3);
            }
        }

        if (yTick > 0)
        {
            for (double y = Math.Ceiling(ax.YMin / yTick) * yTick; y <= ax.YMax + 1e-9; y += yTick)
            {
                float py = ax.ToPixelY(y);
                g.DrawLine(gridPen, ax.Area.Left, py, ax.Area.Right, py);
                string label = Math.Abs(y) < 1e-9 ? "0" : y.ToString("0.##");
                var size = g.MeasureString(label, LabelFont);
                g.DrawString(label, LabelFont, textBrush, ax.Area.Left - size.Width - 4, py - size.Height / 2);
            }
        }

        // Zero line and frame.
        if (ax.YMin < 0 && ax.YMax > 0)
        {
            float py = ax.ToPixelY(0);
            g.DrawLine(axisPen, ax.Area.Left, py, ax.Area.Right, py);
        }
        g.DrawRectangle(axisPen, ax.Area);

        // Labels.
        var xs = g.MeasureString(xLabel, LabelFont);
        g.DrawString(xLabel, LabelFont, textBrush, ax.Area.Left + (ax.Area.Width - xs.Width) / 2, ax.Area.Bottom + 17);

        var state = g.Save();
        g.TranslateTransform(ax.Area.Left - 38, ax.Area.Top + ax.Area.Height / 2f);
        g.RotateTransform(-90);
        var ys = g.MeasureString(yLabel, LabelFont);
        g.DrawString(yLabel, LabelFont, textBrush, -ys.Width / 2, -ys.Height / 2);
        g.Restore(state);

        if (title != null)
            g.DrawString(title, TitleFont, textBrush, ax.Area.Left, ax.Area.Top - 22);
    }

    /// <summary>Sample <paramref name="f"/> across the x range and draw it as a polyline.</summary>
    public static void DrawCurve(Graphics g, Axes ax, Func<double, double> f, Color colour, float width, int samples = 0)
    {
        if (samples <= 0)
            samples = Math.Max(2, ax.Area.Width);

        var pts = new PointF[samples];
        for (int i = 0; i < samples; i++)
        {
            double x = ax.XMin + (ax.XMax - ax.XMin) * i / (samples - 1);
            double y = Math.Clamp(f(x), ax.YMin * 1.5, ax.YMax * 1.5);
            pts[i] = ax.ToPixel(x, y);
        }

        var mode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(colour, width) { LineJoin = LineJoin.Round };
        g.DrawLines(pen, pts);
        g.SmoothingMode = mode;
    }

    public static void DrawLegend(Graphics g, Rectangle area, params (Color colour, string text)[] entries)
    {
        using var brush = new SolidBrush(AxisColour);
        float x = area.Right - 8;
        float y = area.Top + 6;
        // Right-aligned, one entry per line.
        foreach (var (colour, text) in entries)
        {
            var size = g.MeasureString(text, LabelFont);
            using var pen = new Pen(colour, 3f);
            g.DrawString(text, LabelFont, brush, x - size.Width, y);
            g.DrawLine(pen, x - size.Width - 26, y + size.Height / 2, x - size.Width - 8, y + size.Height / 2);
            y += size.Height + 2;
        }
    }

    public static void DrawReadout(Graphics g, Rectangle area, string text)
    {
        using var brush = new SolidBrush(AxisColour);
        g.DrawString(text, LabelFont, brush, area.Left + 6, area.Top + 6);
    }
}
