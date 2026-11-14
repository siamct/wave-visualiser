using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WaveVisualiser.UI;

namespace WaveVisualiser.Modes;

/// <summary>
/// Two point sources in phase on a surface (like two dippers in a ripple
/// tank). Shows either a snapshot of the ripples or the time-averaged
/// intensity, where the lines of constructive and destructive interference
/// stand out.
/// </summary>
public sealed class TwoSourceTab : WaveTab
{
    private const double PictureWidth = 2.0;  // metres across the picture
    private const int Step = 2;        // compute one value per 2x2 block of pixels

    private readonly LabeledSlider _separation, _lambda, _freq;
    private readonly CheckBox _averaged;
    private Bitmap? _buffer;

    public TwoSourceTab() : base("Two sources",
        "Where the path difference to the two sources is a whole number of wavelengths the ripples arrive " +
        "in step and reinforce; where it is an odd number of half wavelengths they cancel. The fringes get " +
        "closer together when the sources are moved apart or the wavelength is reduced.")
    {
        AddHeading("Sources");
        _separation = AddSlider("Separation d", 0.1, 1.2, 0.05, 0.5, "m");
        _lambda = AddSlider("Wavelength λ", 0.05, 0.4, 0.01, 0.15, "m");
        _freq = AddSlider("Frequency", 0.1, 1.0, 0.1, 0.4, "Hz", "0.0");

        AddHeading("Display");
        _averaged = AddCheckBox("Time-averaged intensity", false);
    }

    protected override void Render(Graphics g, Rectangle bounds, double t)
    {
        var area = Rectangle.FromLTRB(bounds.Left + 16, bounds.Top + 28, bounds.Right - 16, bounds.Bottom - 40);
        if (area.Width < 10 || area.Height < 10)
            return;

        int w = area.Width / Step, h = area.Height / Step;
        if (_buffer == null || _buffer.Width != w || _buffer.Height != h)
        {
            _buffer?.Dispose();
            _buffer = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        }

        double metresPerPixel = PictureWidth / w;
        double d = _separation.Value, lambda = _lambda.Value;
        double k = 2 * Math.PI / lambda;
        double omega = 2 * Math.PI * _freq.Value;

        // Sources sit near the bottom edge, either side of the centre.
        double s1x = PictureWidth / 2 - d / 2, s2x = PictureWidth / 2 + d / 2;
        double sy = (h - 4) * metresPerPixel;
        bool averaged = _averaged.Checked;

        var data = _buffer.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var row = new byte[data.Stride];
            for (int py = 0; py < h; py++)
            {
                double y = py * metresPerPixel;
                for (int px = 0; px < w; px++)
                {
                    double x = px * metresPerPixel;
                    double r1 = Math.Sqrt((x - s1x) * (x - s1x) + (y - sy) * (y - sy));
                    double r2 = Math.Sqrt((x - s2x) * (x - s2x) + (y - sy) * (y - sy));

                    double v;
                    if (averaged)
                    {
                        // Time-averaged intensity of two equal waves: proportional to cos²(k Δr / 2).
                        double c = Math.Cos(k * (r1 - r2) / 2);
                        v = c * c; // 0..1
                    }
                    else
                    {
                        // Snapshot of the displacement, mapped from [-2, 2] to [0, 1].
                        double s = Math.Sin(k * r1 - omega * t) + Math.Sin(k * r2 - omega * t);
                        v = (s + 2) / 4;
                    }

                    byte shade = (byte)(30 + v * 215);
                    int i = px * 3;
                    row[i] = shade;                      // B
                    row[i + 1] = (byte)(shade * 0.95);   // G
                    row[i + 2] = (byte)(shade * 0.85);   // R  (slightly warm grey)
                }
                Marshal.Copy(row, 0, data.Scan0 + py * data.Stride, data.Stride);
            }
        }
        finally
        {
            _buffer.UnlockBits(data);
        }

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
        g.DrawImage(_buffer, area);
        using (var framePen = new Pen(Plotting.AxisColour))
            g.DrawRectangle(framePen, area);

        // Mark the sources.
        using (var brush = new SolidBrush(Plotting.EnvelopeColour))
        {
            foreach (double sx in new[] { s1x, s2x })
            {
                float fx = area.Left + (float)(sx / metresPerPixel * Step);
                float fy = area.Top + (float)(sy / metresPerPixel * Step);
                g.FillEllipse(brush, fx - 4, fy - 4, 8, 8);
            }
        }

        using var titleBrush = new SolidBrush(Plotting.AxisColour);
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        g.DrawString(averaged ? "Two-source interference: time-averaged intensity" : "Two-source interference: snapshot",
            titleFont, titleBrush, area.Left, area.Top - 22);

        // Readout and scale bar go in the strip under the picture, where they stay readable.
        int maxima = 2 * (int)Math.Floor(d / lambda) + 1;   // d sin(theta) = m lambda, so |m| <= d / lambda
        var strip = new Rectangle(area.Left - 6, area.Bottom + 2, area.Width, 30);
        Plotting.DrawReadout(g, strip, $"d/λ = {d / lambda:0.00}, so {maxima} lines of maxima (d sin θ = mλ)");

        float barLen = (float)(0.5 / metresPerPixel * Step);
        using var barPen = new Pen(Plotting.AxisColour, 3f);
        using var barFont = new Font("Segoe UI", 9f);
        float barY = area.Bottom + 16;
        g.DrawLine(barPen, area.Right - barLen, barY, area.Right, barY);
        var label = g.MeasureString("0.5 m", barFont);
        g.DrawString("0.5 m", barFont, titleBrush, area.Right - barLen - label.Width - 6, barY - label.Height / 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _buffer?.Dispose();
        base.Dispose(disposing);
    }
}
