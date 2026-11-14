using WaveVisualiser.UI;

namespace WaveVisualiser.Modes;

/// <summary>
/// Standing waves on a string fixed at both ends. Only wavelengths that fit a
/// whole number of half-waves into the string survive, giving the harmonics.
/// </summary>
public sealed class HarmonicsTab : WaveTab
{
    private const double Length = 1.0; // metres

    private readonly LabeledSlider _n, _f1, _amp;
    private readonly CheckBox _showTravelling, _showNodes;

    public HarmonicsTab() : base("Harmonics",
        "A wave reflects from each fixed end and interferes with itself. The string can only hold " +
        "patterns with a node at both ends, so λn = 2L / n and fn = n f1. The standing wave is the " +
        "sum of two equal waves travelling in opposite directions.")
    {
        AddHeading("String");
        _n = AddSlider("Harmonic n", 1, 8, 1, 3, "", "0");
        _f1 = AddSlider("Fundamental f1", 0.1, 1.0, 0.1, 0.3, "Hz", "0.0");
        _amp = AddSlider("Amplitude", 0.1, 1.0, 0.05, 0.6, "m");

        AddHeading("Display");
        _showNodes = AddCheckBox("Mark nodes", true);
        _showTravelling = AddCheckBox("Show travelling components", false);
    }

    protected override void Render(Graphics g, Rectangle bounds, double t)
    {
        int n = (int)_n.Value;
        double f = n * _f1.Value;
        double lambda = 2 * Length / n;
        double k = 2 * Math.PI / lambda;
        double omega = 2 * Math.PI * f;
        double a = _amp.Value;

        var ax = new Axes(Plotting.Inset(bounds), 0, Length, -1.1, 1.1);
        Plotting.DrawAxes(g, ax, "Position along string x (m)", "Displacement y (m)", $"Harmonic n = {n}", xTick: 0.125, yTick: 0.5);

        // Faint outline of the maximum displacement so the nodes and antinodes are obvious.
        Plotting.DrawCurve(g, ax, x => a * Math.Sin(k * x), Plotting.GridColour, 1f);
        Plotting.DrawCurve(g, ax, x => -a * Math.Sin(k * x), Plotting.GridColour, 1f);

        if (_showTravelling.Checked)
        {
            // Each travelling wave has half the amplitude: sin(kx - wt) + sin(kx + wt) = 2 sin(kx) cos(wt)
            Plotting.DrawCurve(g, ax, x => 0.5 * a * Math.Sin(k * x - omega * t), Plotting.Wave1Colour, 1.2f);
            Plotting.DrawCurve(g, ax, x => 0.5 * a * Math.Sin(k * x + omega * t), Plotting.Wave2Colour, 1.2f);
        }

        Plotting.DrawCurve(g, ax, x => a * Math.Sin(k * x) * Math.Cos(omega * t), Plotting.SumColour, 2.5f);

        if (_showNodes.Checked)
        {
            using var brush = new SolidBrush(Plotting.NodeColour);
            for (int i = 0; i <= n; i++)
            {
                var p = ax.ToPixel(i * Length / n, 0);
                g.FillEllipse(brush, p.X - 4, p.Y - 4, 8, 8);
            }
        }

        // Fixed ends.
        using (var endPen = new Pen(Plotting.AxisColour, 4f))
        {
            g.DrawLine(endPen, ax.ToPixelX(0), ax.Area.Top, ax.ToPixelX(0), ax.Area.Bottom);
            g.DrawLine(endPen, ax.ToPixelX(Length), ax.Area.Top, ax.ToPixelX(Length), ax.Area.Bottom);
        }

        Plotting.DrawReadout(g, ax.Area,
            $"λ{n} = 2L/n = {lambda:0.000} m,  f{n} = n f1 = {f:0.00} Hz,  {n + 1} nodes, {n} antinodes");

        if (_showTravelling.Checked)
            Plotting.DrawLegend(g, ax.Area,
                (Plotting.Wave1Colour, "travelling right"),
                (Plotting.Wave2Colour, "travelling left"),
                (Plotting.SumColour, "standing wave"));
    }
}
