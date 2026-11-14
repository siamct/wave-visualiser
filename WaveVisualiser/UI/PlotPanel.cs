namespace WaveVisualiser.UI;

/// <summary>
/// A double-buffered drawing surface. Whoever owns it supplies a renderer
/// callback; the panel just calls it on every paint.
/// </summary>
public sealed class PlotPanel : Control
{
    public PlotPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    public Action<Graphics, Rectangle>? Renderer { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer?.Invoke(e.Graphics, ClientRectangle);
    }

    /// <summary>Render into a bitmap of the given size (used by the image exporter).</summary>
    public Bitmap RenderToBitmap(Size size)
    {
        var bmp = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(BackColor);
        Renderer?.Invoke(g, new Rectangle(Point.Empty, size));
        return bmp;
    }
}
