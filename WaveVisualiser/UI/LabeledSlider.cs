namespace WaveVisualiser.UI;

/// <summary>
/// A TrackBar that works in real units: a caption shows the name and the
/// current value, and <see cref="Value"/> maps the integer position to a
/// double between Min and Max in steps of Step.
/// </summary>
public sealed class LabeledSlider : UserControl
{
    private readonly Label _caption = new() { AutoSize = true, Location = new Point(0, 0) };
    private readonly TrackBar _bar = new() { Location = new Point(0, 18), TickStyle = TickStyle.None, AutoSize = false, Height = 26 };
    private readonly string _name;
    private readonly string _unit;
    private readonly string _format;

    public LabeledSlider(string name, double min, double max, double step, double value, string unit = "", string format = "0.##")
    {
        _name = name;
        _unit = unit;
        _format = format;
        Min = min;
        Step = step;

        _bar.Minimum = 0;
        _bar.Maximum = (int)Math.Round((max - min) / step);
        _bar.TickFrequency = Math.Max(1, _bar.Maximum / 10);
        _bar.ValueChanged += (_, _) =>
        {
            UpdateCaption();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        };

        Height = 48;
        Width = 220;
        Controls.Add(_caption);
        Controls.Add(_bar);
        Value = value;
        UpdateCaption();
    }

    public double Min { get; }
    public double Step { get; }

    public double Value
    {
        get => Min + _bar.Value * Step;
        set => _bar.Value = Math.Clamp((int)Math.Round((value - Min) / Step), _bar.Minimum, _bar.Maximum);
    }

    public event EventHandler? ValueChanged;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        _bar.Width = Width;
    }

    private void UpdateCaption() =>
        _caption.Text = $"{_name}: {Value.ToString(_format)}{(_unit.Length > 0 ? " " + _unit : "")}";
}
