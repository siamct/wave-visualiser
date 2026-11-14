using System.Drawing.Imaging;

namespace WaveVisualiser.UI;

/// <summary>
/// Renders one PNG per tab and a screenshot of the main window into a folder.
/// Usage: WaveVisualiser --export docs
/// </summary>
public static class ImageExport
{
    public static int Run(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);

            using var form = new MainForm { StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
            form.Show();
            Application.DoEvents();

            // A time that shows each effect clearly.
            double[] times = { 0.4, 0.0, 0.0, 0.0 };
            string[] names = { "superposition", "beats", "harmonics", "two_source" };

            for (int i = 0; i < form.Pages.Length; i++)
            {
                form.Freeze(i, times[i]);
                Application.DoEvents();
                using var bmp = form.Pages[i].RenderToBitmap(new Size(820, 500));
                string path = Path.Combine(folder, names[i] + ".png");
                bmp.Save(path, ImageFormat.Png);
                Console.WriteLine($"-> {path}");
            }

            form.Freeze(2, 0.0);
            Application.DoEvents();
            using (var bmp = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.Size));
                // DrawToBitmap on the form alone leaves nested panels blank; draw every child explicitly.
                var clientOrigin = form.PointToScreen(Point.Empty);
                var offset = new Point(clientOrigin.X - form.Location.X, clientOrigin.Y - form.Location.Y);
                DrawChildren(form, form, bmp, offset);
                string path = Path.Combine(folder, "window.png");
                bmp.Save(path, ImageFormat.Png);
                Console.WriteLine($"-> {path}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Export failed: {ex.Message}");
            return 1;
        }
    }

    private static void DrawChildren(Form form, Control parent, Bitmap target, Point formOffset)
    {
        var formScreen = form.PointToScreen(Point.Empty);
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible || child.Width <= 0 || child.Height <= 0)
                continue;
            var screen = child.PointToScreen(Point.Empty);
            var at = new Point(screen.X - formScreen.X + formOffset.X, screen.Y - formScreen.Y + formOffset.Y);
            child.DrawToBitmap(target, new Rectangle(at, child.Size));
            DrawChildren(form, child, target, formOffset);
        }
    }
}
