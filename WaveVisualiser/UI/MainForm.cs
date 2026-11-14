using WaveVisualiser.Modes;

namespace WaveVisualiser.UI;

public sealed class MainForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private readonly Button _playButton = new() { Text = "Pause", Width = 70 };
    private readonly Button _resetButton = new() { Text = "Reset time", Width = 80 };
    private readonly TrackBar _speedBar = new() { Minimum = 1, Maximum = 20, Value = 10, TickFrequency = 5, Width = 160, AutoSize = false, Height = 30 };
    private readonly Label _speedLabel = new() { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private readonly Label _timeLabel = new() { AutoSize = true, Padding = new Padding(16, 8, 0, 0) };

    private readonly WaveTab[] _pages =
    {
        new SuperpositionTab(),
        new BeatsTab(),
        new HarmonicsTab(),
        new TwoSourceTab(),
    };

    private DateTime _lastTick = DateTime.Now;
    private double _time;

    public MainForm()
    {
        Text = "Wave Visualiser";
        ClientSize = new Size(1100, 680);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;

        foreach (var page in _pages)
        {
            var tab = new TabPage(page.Title);
            tab.Controls.Add(page);
            _tabs.TabPages.Add(tab);
        }

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(6, 4, 6, 0),
            FlowDirection = FlowDirection.LeftToRight,
        };
        toolbar.Controls.Add(_playButton);
        toolbar.Controls.Add(_resetButton);
        toolbar.Controls.Add(new Label { Text = "Speed", AutoSize = true, Padding = new Padding(12, 8, 0, 0) });
        toolbar.Controls.Add(_speedBar);
        toolbar.Controls.Add(_speedLabel);
        toolbar.Controls.Add(_timeLabel);

        Controls.Add(_tabs);
        Controls.Add(toolbar);

        _playButton.Click += (_, _) => TogglePlay();
        _resetButton.Click += (_, _) => { _time = 0; RedrawCurrent(); };
        _speedBar.ValueChanged += (_, _) => UpdateSpeedLabel();
        _tabs.SelectedIndexChanged += (_, _) => RedrawCurrent();
        _timer.Tick += (_, _) => OnTick();

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space) { TogglePlay(); e.Handled = true; }
            if (e.KeyCode == Keys.R) { _time = 0; RedrawCurrent(); }
        };

        UpdateSpeedLabel();
        _timer.Start();
    }

    /// <summary>Simulation speed as a multiple of real time.</summary>
    private double Speed => _speedBar.Value / 10.0;

    public WaveTab[] Pages => _pages;

    public double Time
    {
        get => _time;
        set { _time = value; foreach (var p in _pages) p.Time = value; }
    }

    private void OnTick()
    {
        var now = DateTime.Now;
        double dt = (now - _lastTick).TotalSeconds;
        _lastTick = now;
        _time += Math.Min(dt, 0.1) * Speed;
        RedrawCurrent();
    }

    private void RedrawCurrent()
    {
        if (_tabs.SelectedTab?.Controls[0] is WaveTab page)
        {
            page.Time = _time;
            page.Redraw();
        }
        _timeLabel.Text = $"t = {_time:0.00} s";
    }

    private void TogglePlay()
    {
        if (_timer.Enabled)
        {
            _timer.Stop();
            _playButton.Text = "Play";
        }
        else
        {
            _lastTick = DateTime.Now;
            _timer.Start();
            _playButton.Text = "Pause";
        }
    }

    private void UpdateSpeedLabel() => _speedLabel.Text = $"{Speed:0.0}x";

    /// <summary>Used by the exporter: stop the clock and select a tab.</summary>
    internal void Freeze(int tabIndex, double time)
    {
        _timer.Stop();
        _playButton.Text = "Play";
        Time = time;
        _tabs.SelectedIndex = tabIndex;
        RedrawCurrent();
    }
}
