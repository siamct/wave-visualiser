using System;
using System.Drawing;
using System.Windows.Forms;

namespace WaveVisualiser
{
    public enum Mode { Superposition, Beats, StandingWave }

    // Draws the waves. Each wave is one polyline of a few hundred points, which is
    // plenty smooth and cheap enough to redraw 30 times a second.
    public class WavePanel : Control
    {
        public Mode Mode = Mode.Superposition;

        // wave 1 and wave 2: amplitude, wavelength (or frequency for beats), phase
        public double A1 = 1, A2 = 1;
        public double Lambda1 = 4, Lambda2 = 4;      // in the same units as the x axis (0..20)
        public double F1 = 1, F2 = 1;                // Hz
        public double Phase2 = 0;                    // degrees
        public int Harmonic = 3;
        public bool ShowParts = true;
        public double Time;

        private const int Points = 400;
        private const double XMax = 20;
        private readonly PointF[] pts = new PointF[Points];

        public static readonly Color Back = Color.FromArgb(153, 180, 209);
        private static readonly Pen Wave1 = new Pen(Color.FromArgb(40, 110, 200), 1.5f);
        private static readonly Pen Wave2 = new Pen(Color.FromArgb(40, 160, 60), 1.5f);
        private static readonly Pen Result = new Pen(Color.FromArgb(220, 40, 40), 2.5f);
        private static readonly Pen Axis = new Pen(Color.FromArgb(120, 0, 0, 0));
        private static readonly Pen Envelope = new Pen(Color.Gray, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        public WavePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Back;
        }

        // y of each wave at position x and the current time
        private double Y1(double x)
        {
            if (Mode == Mode.Beats) return A1 * Math.Sin(2 * Math.PI * F1 * (x / 4 + Time));
            if (Mode == Mode.StandingWave) return StandingPart(x, +1);
            return A1 * Math.Sin(2 * Math.PI * (x / Lambda1 - F1 * Time));
        }

        private double Y2(double x)
        {
            if (Mode == Mode.Beats) return A2 * Math.Sin(2 * Math.PI * F2 * (x / 4 + Time));
            if (Mode == Mode.StandingWave) return StandingPart(x, -1);
            double phase = Phase2 * Math.PI / 180;
            return A2 * Math.Sin(2 * Math.PI * (x / Lambda2 - F2 * Time) + phase);
        }

        // standing wave = two identical waves going opposite ways
        private double StandingPart(double x, int direction)
        {
            double lambda = 2 * XMax / Harmonic;               // L = n * lambda / 2
            return 0.5 * A1 * Math.Sin(2 * Math.PI * (x / lambda - direction * F1 * Time));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int pad = 30;
            float w = Width - 2 * pad, mid = Height / 2f;
            float scale = (Height / 2f - pad) / 2.2f;           // amplitude 2.2 fills the panel

            // white plot area like the grid in the pathfinder
            g.FillRectangle(Brushes.White, pad, pad, w, Height - 2 * pad);
            g.DrawRectangle(Pens.Black, pad, pad, w, Height - 2 * pad);
            g.DrawLine(Axis, pad, mid, pad + w, mid);

            // in beats mode the x axis is time (5 s across), otherwise it's distance
            Func<int, double> xAt = i => XMax * i / (Points - 1);

            if (ShowParts)
            {
                Draw(g, Wave1, i => Y1(xAt(i)), pad, w, mid, scale);
                Draw(g, Wave2, i => Y2(xAt(i)), pad, w, mid, scale);
            }
            Draw(g, Result, i => Y1(xAt(i)) + Y2(xAt(i)), pad, w, mid, scale);

            if (Mode == Mode.Beats)
            {
                // envelope: |A1 + A2 e^(i dw t)|, the slow wobble you hear
                double dw = 2 * Math.PI * (F1 - F2);
                Func<int, double> env = i => Math.Sqrt(A1 * A1 + A2 * A2 + 2 * A1 * A2 * Math.Cos(dw * (xAt(i) / 4 + Time)));
                Draw(g, Envelope, env, pad, w, mid, scale);
                Draw(g, Envelope, i => -env(i), pad, w, mid, scale);
            }

            if (Mode == Mode.StandingWave)
            {
                // nodes: the points that never move
                for (int n = 0; n <= Harmonic; n++)
                {
                    float x = pad + w * n / Harmonic;
                    g.FillEllipse(Brushes.Black, x - 4, mid - 4, 8, 8);
                }
            }
        }

        private void Draw(Graphics g, Pen pen, Func<int, double> y, float pad, float w, float mid, float scale)
        {
            for (int i = 0; i < Points; i++)
                pts[i] = new PointF(pad + w * i / (Points - 1), mid - (float)(y(i) * scale));
            g.DrawLines(pen, pts);
        }
    }
}
