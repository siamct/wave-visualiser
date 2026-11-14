using WaveVisualiser.UI;

namespace WaveVisualiser.Modes;

/// <summary>
/// Base for each tab: a plot on the left, a column of controls on the right
/// and a short explanation underneath. Subclasses add their sliders and
/// implement <see cref="Render"/> for a given simulation time.
/// </summary>
public abstract class WaveTab : UserControl
{
    protected readonly PlotPanel Plot = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _controls = new()
    {
        Dock = DockStyle.Right,
        Width = 250,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(10, 8, 6, 8),
    };
    private readonly Label _explanation = new()
    {
        Dock = DockStyle.Bottom,
        Height = 54,
        Padding = new Padding(10, 6, 10, 6),
        ForeColor = Color.FromArgb(70, 70, 70),
    };

    protected WaveTab(string title, string explanation)
    {
        Title = title;
        Dock = DockStyle.Fill;
        _explanation.Text = explanation;

        Controls.Add(Plot);
        Controls.Add(_controls);
        Controls.Add(_explanation);

        Plot.Renderer = (g, r) => Render(g, r, Time);
    }

    public string Title { get; }

    /// <summary>Simulation time in seconds, set by the main form's timer.</summary>
    public double Time { get; set; }

    public void Redraw() => Plot.Invalidate();

    public Bitmap RenderToBitmap(Size size) => Plot.RenderToBitmap(size);

    protected abstract void Render(Graphics g, Rectangle bounds, double t);

    // ----- helpers for building the control column -----

    protected LabeledSlider AddSlider(string name, double min, double max, double step, double value, string unit = "", string format = "0.##")
    {
        var s = new LabeledSlider(name, min, max, step, value, unit, format) { Width = 220 };
        s.ValueChanged += (_, _) => Redraw();
        _controls.Controls.Add(s);
        return s;
    }

    protected CheckBox AddCheckBox(string text, bool isChecked = false)
    {
        var c = new CheckBox { Text = text, AutoSize = true, Checked = isChecked, Margin = new Padding(3, 6, 3, 3) };
        c.CheckedChanged += (_, _) => Redraw();
        _controls.Controls.Add(c);
        return c;
    }

    protected void AddHeading(string text)
    {
        _controls.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 10, 3, 2),
        });
    }

    protected void AddButton(string text, Action onClick)
    {
        var b = new Button { Text = text, Width = 120, Margin = new Padding(3, 8, 3, 3) };
        b.Click += (_, _) => { onClick(); Redraw(); };
        _controls.Controls.Add(b);
    }
}
