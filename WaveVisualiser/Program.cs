using WaveVisualiser.UI;

namespace WaveVisualiser;

internal static class Program
{
    /// <summary>
    /// Entry point. Normally opens the main window.
    ///
    /// Command line:
    ///   WaveVisualiser --export <folder>
    /// renders one PNG per tab plus a screenshot of the window into the folder
    /// and exits. Used to make the images in the README.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length >= 2 && args[0] == "--export")
            return ImageExport.Run(args[1]);

        Application.Run(new MainForm());
        return 0;
    }
}
