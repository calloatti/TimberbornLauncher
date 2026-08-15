using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TimberbornLauncher;

/// <summary>
/// Captures the command line Steam passes to the launcher via launch options:
/// "path\to\TimberbornLauncher.exe" %command%
/// %command% is the game executable followed by any game arguments.
/// </summary>
public static class LaunchOptions
{
    private static List<string> _arguments = new();

    public static string GameExecutablePath { get; private set; } = "";

    public static IReadOnlyList<string> Arguments => _arguments;

    public static void Initialize(string[] args)
    {
        _arguments = new List<string>(args);
        GameExecutablePath = args.Length > 0 ? args[0].Trim('"') : "";
    }

    public static string GetGameDirectory()
    {
        if (string.IsNullOrEmpty(GameExecutablePath))
        {
            return "";
        }
        return Path.GetDirectoryName(GameExecutablePath) ?? "";
    }

    /// <summary>
    /// Game arguments to forward, i.e. everything after the game executable.
    /// </summary>
    public static IEnumerable<string> GetGameArguments()
    {
        return _arguments.Skip(1);
    }

    /// <summary>
    /// Whether the launcher was invoked in a way that should skip the mod-manager UI
    /// and launch the game directly: the game's own -skipModManager flag was forwarded,
    /// or both -settlementName and -saveName were passed (a specific save/load target).
    /// </summary>
    public static bool ShouldLaunchGameDirectly()
    {
        List<string> args = _arguments;
        if (args.Skip(1).Any(a => string.Equals(a, "-skipModManager", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        bool hasSettlement = args.Skip(1).Any(a => string.Equals(a, "-settlementName", StringComparison.OrdinalIgnoreCase));
        bool hasSave = args.Skip(1).Any(a => string.Equals(a, "-saveName", StringComparison.OrdinalIgnoreCase));
        return hasSettlement && hasSave;
    }
}



