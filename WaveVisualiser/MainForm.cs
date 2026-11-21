using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WaveVisualiser
{
    // Same layout as my pathfinder: plain buttons and number boxes down the left,
    // the drawing on the right, everything on the blue form.
    public class MainForm : Form
    {
        private readonly WavePanel panel = new WavePanel();
        private readonly Timer timer = new Timer { Interval = 33 };     // ~30 fps is plenty
        private readonly List<Button> modeButtons = new List<Button>();
        private NumericUpDown a1, a2, lambda1, lambda2, f1, f2, phase, harmonic;
        private TrackBar speed;
        private Button pauseButton;
        private Label readout;
        private int y;
        private Panel side;

        public MainForm()
        {
            Text = "Wave Visualiser";
            ClientSize = new Size(1000, 600);
            MinimumSize = new Size(760, 520);
            BackColor = WavePanel.Back;
            Font = new Font("Segoe UI", 9f);
            KeyPreview = true;

            side = new Panel { Dock = DockStyle.Left, Width = 190, AutoScroll = true };
            y = 12;

            Heading("Mode");
            string[] modes = { "Superposition", "Beats", "Standing wave" };
            for (int i = 0; i < modes.Length; i++)
            {
                Mode m = (Mode)i;
                var b = Btn(modes[i], 12, y, 165, (s, e) => SetMode(m));
                modeButtons.Add(b);
                y += 27;
            }
            y += 6;

            Heading("Wave 1 (blue)");
            a1 = Number("Amplitude", 0, 2, 1, 0.1m);
            lambda1 = Number("Wavelength", 1, 20, 4, 0.5m);
            f1 = Number("Frequency (Hz)", 0, 5, 1, 0.1m);

            Heading("Wave 2 (green)");
            a2 = Number("Amplitude", 0, 2, 1, 0.1m);
            lambda2 = Number("Wavelength", 1, 20, 4, 0.5m);
            f2 = Number("Frequency (Hz)", 0, 5, 1, 0.1m);
            phase = Number("Phase (deg)", 0, 360, 0, 15);
            harmonic = Number("Harmonic n", 1, 8, 3, 1);

            var parts = new CheckBox { Text = "Show both waves", Checked = true, Location = new Point(12, y), AutoSize = true };
            parts.CheckedChanged += (s, e) => { panel.ShowParts = parts.Checked; panel.Invalidate(); };
            side.Controls.Add(parts);
            y += 26;

            Heading("Speed");
            speed = new TrackBar { Minimum = 1, Maximum = 10, Value = 5, Location = new Point(6, y), Width = 175, TickStyle = TickStyle.None };
            side.Controls.Add(speed);
            y += 46;
            pauseButton = Btn("Pause", 12, y, 80, (s, e) => Pause());
            Btn("Reset", 97, y, 80, (s, e) => { panel.Time = 0; panel.Invalidate(); });
            y += 30;

            readout = new Label { Location = new Point(12, y), Size = new Size(170, 80) };
            side.Controls.Add(readout);

            panel.Dock = DockStyle.Fill;
            Controls.Add(panel);
            Controls.Add(side);

            foreach (var n in new[] { a1, a2, lambda1, lambda2, f1, f2, phase, harmonic })
                n.ValueChanged += (s, e) => ReadValues();

            timer.Tick += (s, e) =>
            {
                panel.Time += 0.033 * speed.Value / 5.0;
                panel.Invalidate();
            };
            KeyDown += (s, e) =>
            {
                if (ActiveControl is NumericUpDown) return;
                if (e.KeyCode == Keys.Space) { Pause(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.R) panel.Time = 0;
            };

            SetMode(Mode.Superposition);
            timer.Start();
        }

        private void SetMode(Mode m)
        {
            panel.Mode = m;
            for (int i = 0; i < modeButtons.Count; i++)
                modeButtons[i].BackColor = i == (int)m ? Color.LightSteelBlue : SystemColors.Control;

            // only enable the boxes that mean something in this mode
            bool sup = m == Mode.Superposition, beats = m == Mode.Beats, stand = m == Mode.StandingWave;
            lambda1.Enabled = lambda2.Enabled = phase.Enabled = sup;
            a2.Enabled = f2.Enabled = !stand;
            harmonic.Enabled = stand;
            if (beats && f2.Value == f1.Value) f2.Value = Math.Min(f2.Maximum, f1.Value + 0.4m);
            ReadValues();
        }

        private void ReadValues()
        {
            panel.A1 = (double)a1.Value;
            panel.A2 = (double)a2.Value;
            panel.Lambda1 = (double)lambda1.Value;
            panel.Lambda2 = (double)lambda2.Value;
            panel.F1 = (double)f1.Value;
            panel.F2 = (double)f2.Value;
            panel.Phase2 = (double)phase.Value;
            panel.Harmonic = (int)harmonic.Value;
            readout.Text = Readout();
            panel.Invalidate();
        }

        // one or two lines of the physics for the current mode
        private string Readout()
        {
            switch (panel.Mode)
            {
                case Mode.Beats:
                    double fb = Math.Abs(panel.F1 - panel.F2);
                    return string.Format("Beat frequency = |f1 - f2|\n= {0:0.0} Hz", fb) +
                           (fb > 0 ? string.Format("\nBeat period = {0:0.00} s", 1 / fb) : "\nNo beats (same frequency)");
                case Mode.StandingWave:
                    int n = panel.Harmonic;
                    return string.Format("Harmonic {0}\nwavelength = 2L / n = {1:0.0}\nnodes {2}, antinodes {3}", n, 40.0 / n, n + 1, n);
                default:
                    double phi = panel.Phase2 * Math.PI / 180;
                    double a = Math.Sqrt(panel.A1 * panel.A1 + panel.A2 * panel.A2 + 2 * panel.A1 * panel.A2 * Math.Cos(phi));
                    string kind = panel.Phase2 % 360 == 0 ? "in phase (constructive)" : panel.Phase2 % 360 == 180 ? "antiphase (destructive)" : "partly in phase";
                    string note = panel.Lambda1 != panel.Lambda2 || panel.F1 != panel.F2 ? "\n(only exact when both waves match)" : "";
                    return string.Format("Phase difference {0:0} deg\n{1}\nresultant amplitude = {2:0.00}{3}", panel.Phase2, kind, a, note);
            }
        }

        private void Pause()
        {
            timer.Enabled = !timer.Enabled;
            pauseButton.Text = timer.Enabled ? "Pause" : "Play";
        }

        private void Heading(string text)
        {
            side.Controls.Add(new Label { Text = text, Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            y += 20;
        }

        private NumericUpDown Number(string text, decimal min, decimal max, decimal value, decimal step)
        {
            side.Controls.Add(new Label { Text = text, Location = new Point(12, y + 3), AutoSize = true });
            var n = new NumericUpDown
            {
                Minimum = min, Maximum = max, Value = value, Increment = step,
                DecimalPlaces = step < 1 ? 1 : 0, Location = new Point(110, y), Width = 67
            };
            side.Controls.Add(n);
            y += 27;
            return n;
        }

        private Button Btn(string text, int x, int top, int w, EventHandler click)
        {
            var b = new Button { Text = text, Location = new Point(x, top), Size = new Size(w, 23), BackColor = SystemColors.Control, TabStop = false };
            b.Click += click;
            side.Controls.Add(b);
            return b;
        }
    }
}
