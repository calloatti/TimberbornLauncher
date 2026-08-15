using System;
using System.Windows.Forms;
using TimberbornLauncher.Mods;

namespace TimberbornLauncher;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Log.Clear();
        Log.Info($"=== Launcher started (args: {string.Join(" ", args)}) ===");
        LaunchOptions.Initialize(args);
        AppDatabase.Initialize();
        ModScanner.Scan();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new MainForm());
    }
}



