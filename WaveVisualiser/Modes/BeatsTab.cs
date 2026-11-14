using WaveVisualiser.UI;

namespace WaveVisualiser.Modes;

/// <summary>
/// Two tones of slightly different frequency heard at one point. Their sum
/// rises and falls in loudness at the beat frequency |f1 - f2|.
/// </summary>
public sealed class BeatsTab : WaveTab
{
    private readonly LabeledSlider _f1, _f2, _window;
    private readonly CheckBox _showEnvelope, _showComponents;

    public BeatsTab() : base("Beats",
        "Adding two waves of nearly equal frequency gives a wave at the average frequency whose amplitude " +
        "is modulated by a slow envelope. The loudness peaks |f1 - f2| times per second: the beat frequency. " +
        "Used for tuning instruments.")
    {
        AddHeading("Tones");
        _f1 = AddSlider("Frequency f1", 1, 30, 0.5, 10, "Hz", "0.0");
        _f2 = AddSlider("Frequency f2", 1, 30, 0.5, 11, "Hz", "0.0");

        AddHeading("Display");
        _window = AddSlider("Time shown", 0.5, 4, 0.5, 2, "s", "0.0");
        _showEnvelope = AddCheckBox("Show beat envelope", true);
        _showComponents = AddCheckBox("Show the two tones", false);
    }

    protected override void Render(Graphics g, Rectangle bounds, double t)
    {
        double f1 = _f1.Value, f2 = _f2.Value;
        double window = _window.Value;

        var ax = new Axes(Plotting.Inset(bounds), 0, window, -2.2, 2.2);
        Plotting.DrawAxes(g, ax, "Time (s)", "Displacement (arbitrary units)", "Beats", xTick: 0.25, yTick: 1);

        // The plot scrolls: the x axis shows a window of time starting at the current time.
        double Tone1(double x) => Math.Sin(2 * Math.PI * f1 * (x + t));
        double Tone2(double x) => Math.Sin(2 * Math.PI * f2 * (x + t));

        // sin(a) + sin(b) = 2 cos((a-b)/2) sin((a+b)/2): the first factor is the envelope.
        double Envelope(double x) => 2 * Math.Abs(Math.Cos(Math.PI * (f1 - f2) * (x + t)));

        if (_showComponents.Checked)
        {
            Plotting.DrawCurve(g, ax, Tone1, Plotting.Wave1Colour, 1f, samples: 2000);
            Plotting.DrawCurve(g, ax, Tone2, Plotting.Wave2Colour, 1f, samples: 2000);
        }

        Plotting.DrawCurve(g, ax, x => Tone1(x) + Tone2(x), Plotting.SumColour, 1.8f, samples: 2500);

        if (_showEnvelope.Checked)
        {
            Plotting.DrawCurve(g, ax, Envelope, Plotting.EnvelopeColour, 1.5f, samples: 1500);
            Plotting.DrawCurve(g, ax, x => -Envelope(x), Plotting.EnvelopeColour, 1.5f, samples: 1500);
        }

        double beat = Math.Abs(f1 - f2);
        string readout = beat < 1e-9
            ? "Same frequency: no beats"
            : $"Beat frequency |f1 - f2| = {beat:0.0} Hz, one beat every {1 / beat:0.00} s; average frequency {(f1 + f2) / 2:0.00} Hz";
        Plotting.DrawReadout(g, ax.Area, readout);

        Plotting.DrawLegend(g, ax.Area,
            (Plotting.SumColour, "sum"),
            (Plotting.EnvelopeColour, "envelope"));
    }
}
