using System.Windows;

namespace DeckMiner.Gui;

/// <summary>
/// WPF Application entry point
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure UTF-8 encoding for console output (in case we need it)
        try
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (System.IO.IOException)
        {
            // Ignore if console is not available
        }
    }
}
