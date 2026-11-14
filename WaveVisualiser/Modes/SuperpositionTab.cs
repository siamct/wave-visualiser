using WaveVisualiser.UI;

namespace WaveVisualiser.Modes;

/// <summary>
/// Two travelling waves on the same line and their sum. With equal wavelength
/// and frequency the phase slider shows constructive and destructive
/// interference; sending wave 2 the other way produces a standing wave.
/// </summary>
public sealed class SuperpositionTab : WaveTab
{
    private const double Length = 2.0; // metres shown on the x axis

    private readonly LabeledSlider _a1, _a2, _lambda1, _lambda2, _f1, _f2, _phase;
    private readonly CheckBox _opposite, _showComponents;

    public SuperpositionTab() : base("Superposition",
        "The displacement at each point is the sum of the two waves (principle of superposition). " +
        "Equal waves in phase add up (constructive); 180 degrees apart they cancel (destructive). " +
        "Tick 'wave 2 travels left' to see a standing wave form.")
    {
        AddHeading("Wave 1");
        _a1 = AddSlider("Amplitude A1", 0, 1, 0.05, 0.8, "m");
        _lambda1 = AddSlider("Wavelength λ1", 0.2, 2.0, 0.05, 0.8, "m");
        _f1 = AddSlider("Frequency f1", 0.1, 2.0, 0.1, 0.5, "Hz");

        AddHeading("Wave 2");
        _a2 = AddSlider("Amplitude A2", 0, 1, 0.05, 0.8, "m");
        _lambda2 = AddSlider("Wavelength λ2", 0.2, 2.0, 0.05, 0.8, "m");
        _f2 = AddSlider("Frequency f2", 0.1, 2.0, 0.1, 0.5, "Hz");
        _phase = AddSlider("Phase lead of wave 2", 0, 360, 15, 0, "°", "0");
        _opposite = AddCheckBox("Wave 2 travels left");

        AddHeading("Display");
        _showComponents = AddCheckBox("Show the two waves", true);

        AddButton("Match wave 2 to 1", () =>
        {
            _a2.Value = _a1.Value;
            _lambda2.Value = _lambda1.Value;
            _f2.Value = _f1.Value;
        });
    }

    private double Wave1(double x, double t) =>
        _a1.Value * Math.Sin(2 * Math.PI * (x / _lambda1.Value - _f1.Value * t));

    private double Wave2(double x, double t)
    {
        double direction = _opposite.Checked ? +1 : -1;
        double phase = _phase.Value * Math.PI / 180.0;
        return _a2.Value * Math.Sin(2 * Math.PI * (x / _lambda2.Value + direction * _f2.Value * t) + phase);
    }

    protected override void Render(Graphics g, Rectangle bounds, double t)
    {
        double yMax = Math.Max(0.5, _a1.Value + _a2.Value);
        var ax = new Axes(Plotting.Inset(bounds), 0, Length, -yMax, yMax);
        Plotting.DrawAxes(g, ax, "Position x (m)", "Displacement y (m)", "Superposition of two waves", xTick: 0.25, yTick: 0.5);

        if (_showComponents.Checked)
        {
            Plotting.DrawCurve(g, ax, x => Wave1(x, t), Plotting.Wave1Colour, 1.5f);
            Plotting.DrawCurve(g, ax, x => Wave2(x, t), Plotting.Wave2Colour, 1.5f);
        }
        Plotting.DrawCurve(g, ax, x => Wave1(x, t) + Wave2(x, t), Plotting.SumColour, 2.5f);

        Plotting.DrawLegend(g, ax.Area,
            (Plotting.Wave1Colour, "wave 1"),
            (Plotting.Wave2Colour, "wave 2"),
            (Plotting.SumColour, "resultant"));

        Plotting.DrawReadout(g, ax.Area, Readout());
    }

    private string Readout()
    {
        bool sameShape = Math.Abs(_lambda1.Value - _lambda2.Value) < 1e-9 && Math.Abs(_f1.Value - _f2.Value) < 1e-9;

        if (sameShape && _opposite.Checked)
            return $"Standing wave: nodes every λ/2 = {_lambda1.Value / 2:0.00} m";

        if (!sameShape)
            return "Different wavelengths or frequencies: the pattern changes shape as it moves";

        double phi = _phase.Value * Math.PI / 180.0;
        double resultant = Math.Sqrt(_a1.Value * _a1.Value + _a2.Value * _a2.Value + 2 * _a1.Value * _a2.Value * Math.Cos(phi));
        string kind = _phase.Value % 360 == 0 ? "constructive" : Math.Abs(_phase.Value - 180) < 1e-9 ? "destructive" : "partial";
        return $"Phase difference {_phase.Value:0}°: {kind} interference, resultant amplitude {resultant:0.00} m";
    }
}
