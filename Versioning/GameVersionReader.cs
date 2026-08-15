using System;
using System.IO;

namespace TimberbornLauncher.Versioning;

/// <summary>
/// Reads the installed game version from the game's
/// Timberborn_Data\StreamingAssets\Version.txt file.
/// </summary>
public static class GameVersionReader
{
    private const string VersionFileName = "Version.txt";

    public static GameVersion? TryReadCurrentVersion()
    {
        string exe = ResolveGameExecutable();
        if (string.IsNullOrEmpty(exe))
        {
            return null;
        }
        string? installDir = Path.GetDirectoryName(exe);
        if (string.IsNullOrEmpty(installDir))
        {
            return null;
        }
        string versionFile = Path.Combine(installDir, "Timberborn_Data", "StreamingAssets", VersionFileName);
        if (!File.Exists(versionFile))
        {
            return null;
        }
        try
        {
            string content = File.ReadAllText(versionFile).Trim();
            return GameVersion.TryCreate(content);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ResolveGameExecutable()
    {
        string exe = LaunchOptions.GameExecutablePath;
        if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
        {
            return exe;
        }
        return GameLocator.DiscoverGameExecutable() ?? "";
    }
}
