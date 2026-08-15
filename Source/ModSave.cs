using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TimberbornLauncher.Mods;

namespace TimberbornLauncher;

/// <summary>
/// Launch-only helper. Load-order computation and registry push live in <see cref="ModSorter.Apply"/>.
/// </summary>
public static class ModSave
{
    public static async System.Threading.Tasks.Task ApplyAndLaunchAsync(Form owner)
    {
        Log.Info("ApplyAndLaunchAsync: starting");
        try
        {
            if (!ModSorter.Apply(owner))
            {
                Log.Info("ApplyAndLaunchAsync: Apply returned false, aborting launch");
                return;
            }
            Log.Info("ApplyAndLaunchAsync: Apply succeeded, calling LaunchGameAsync");
            await LaunchGameAsync(owner);
        }
        catch (Exception ex)
        {
            Log.Error("ApplyAndLaunchAsync: unhandled exception", ex);
            MessageBox.Show(owner, "Launch failed:\n" + ex.Message, "Timberborn Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static async System.Threading.Tasks.Task LaunchGameAsync(Form owner)
    {
        string executable = LaunchOptions.GameExecutablePath;
        if (string.IsNullOrEmpty(executable) || !File.Exists(executable))
        {
            executable = GameLocator.DiscoverGameExecutable() ?? "";
        }
        if (string.IsNullOrEmpty(executable) || !File.Exists(executable))
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Timberborn executable|Timberborn.exe|Executable files (*.exe)|*.exe",
                Title = "Select Timberborn.exe"
            };
            if (dialog.ShowDialog(owner) != DialogResult.OK)
            {
                return;
            }
            executable = dialog.FileName;
        }

        string args = string.Join(" ", LaunchOptions.GetGameArguments());
        string workingDir = Path.GetDirectoryName(executable) ?? "";
        Log.Launch(executable, workingDir, args);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };
            foreach (string argument in LaunchOptions.GetGameArguments())
            {
                startInfo.ArgumentList.Add(argument);
            }
            bool launchedDirectly = LaunchOptions.ShouldLaunchGameDirectly();
            if (!launchedDirectly && !startInfo.ArgumentList.Contains("-skipModManager"))
            {
                startInfo.ArgumentList.Add("-skipModManager");
            }

            // Tell Steamworks this is the official launch so the game doesn't
            // warn/relaunch, without relaunching the launcher. steam_appid.txt
            // stays in place (the game reads it; deleting is unnecessary).
            GameLocator.SteamAppIdWriteResult steamAppIdResult = GameLocator.WriteSteamAppIdForGame(executable);
            if (!steamAppIdResult.InGameDirectory)
            {
                string? gameDirectory = Path.GetDirectoryName(executable);
                string destination = gameDirectory != null
                    ? Path.Combine(gameDirectory, "steam_appid.txt")
                    : "the game folder";
                MessageBox.Show(owner,
                    "The launcher couldn't write steam_appid.txt into the game folder (it's protected).\r\n\r\n" +
                    "To suppress Steam's 'launched outside Steam' warning, copy this file:\r\n  " +
                    steamAppIdResult.Path + "\r\ninto the game folder as:\r\n  " + destination,
                    "Timberborn Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Process.Start(startInfo);
            Log.Info($"Launched {executable} with args: {args}");
            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start game: {executable} {args}", ex);
            MessageBox.Show(owner, "Failed to start the game:\n" + ex.Message, "Timberborn Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}


