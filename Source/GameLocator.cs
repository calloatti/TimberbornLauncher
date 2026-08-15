using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace TimberbornLauncher;

/// <summary>
/// Locates the installed Timberborn executable by reading Steam's config
/// (registry SteamPath, libraryfolders.vdf and appmanifest_1062090.acf).
/// Used when the launcher is run directly instead of through Steam's %command%.
/// </summary>
public static class GameLocator
{
    private const uint TimberbornAppId = 1062090;

    /// <summary>Steam app id for Timberborn, written to steam_appid.txt so the game
    /// initializes Steamworks when launched directly (no -appid arg, no relaunch loop).</summary>
    public const string SteamAppId = "1062090";

    private const string SteamAppIdFileName = "steam_appid.txt";

    /// <summary>Result of writing steam_appid.txt next to the game executable.</summary>
    public sealed class SteamAppIdWriteResult
    {
        public bool InGameDirectory { get; init; }

        public string Path { get; init; } = "";
    }

    /// <summary>
    /// Attempts to write steam_appid.txt next to the game executable so SteamAPI
    /// initializes without -appid (which would relaunch the launcher). Left in place.
    /// Returns the result; if the game directory isn't writable (e.g. Program Files
    /// without elevation), returns a fallback entry pointing at the launcher folder
    /// so the caller can prompt the user to copy it into place.
    /// </summary>
    public static SteamAppIdWriteResult WriteSteamAppIdForGame(string gameExecutablePath)
    {
        string? gameDirectory = Path.GetDirectoryName(gameExecutablePath);
        if (string.IsNullOrEmpty(gameDirectory))
        {
            return Fallback();
        }
        string gameFile = Path.Combine(gameDirectory, SteamAppIdFileName);
        if (TryWriteSteamAppIdFile(gameFile, out bool alreadyPresent))
        {
            return new SteamAppIdWriteResult { InGameDirectory = true, Path = gameFile };
        }
        return Fallback();

        SteamAppIdWriteResult Fallback()
        {
            string launcherDirectory = AppContext.BaseDirectory;
            string fallbackFile = Path.Combine(launcherDirectory, SteamAppIdFileName);
            TryWriteSteamAppIdFile(fallbackFile, out _);
            return new SteamAppIdWriteResult { InGameDirectory = false, Path = fallbackFile };
        }
    }

    private static bool TryWriteSteamAppIdFile(string filePath, out bool alreadyPresent)
    {
        alreadyPresent = false;
        string? existing = null;
        try
        {
            existing = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }
        catch
        {
            existing = null;
        }
        if (string.Equals(existing, SteamAppId, StringComparison.Ordinal))
        {
            alreadyPresent = true;
            return true;
        }
        try
        {
            File.WriteAllText(filePath, SteamAppId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private const string GameExecutableName = "Timberborn.exe";

    public static string? DiscoverGameExecutable()
    {
        foreach (string library in GetSteamLibraries())
        {
            string? installdir = TryGetInstallDir(library);
            if (string.IsNullOrEmpty(installdir))
            {
                continue;
            }
            string exe = Path.Combine(library, "steamapps", "common", installdir, GameExecutableName);
            if (File.Exists(exe))
            {
                return exe;
            }
        }
        return null;
    }

    /// <summary>
    /// Workshop content roots for Timberborn across all Steam libraries.
    /// </summary>
    public static IEnumerable<string> GetWorkshopContentRoots()
    {
        foreach (string library in GetSteamLibraries())
        {
            string root = Path.Combine(library, "steamapps", "workshop", "content", TimberbornAppId.ToString());
            if (Directory.Exists(root))
            {
                yield return root;
            }
        }
    }

    public static string? TryGetSteamPath()
    {
        object? steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
        return steamPath as string;
    }

    /// <summary>
    /// Returns the primary Steam library plus any extra libraries from libraryfolders.vdf.
    /// </summary>
    public static IEnumerable<string> GetSteamLibraries()
    {
        string? steamRoot = TryGetSteamPath();
        if (string.IsNullOrEmpty(steamRoot))
        {
            yield break;
        }
        var libraries = new List<string> { NormalizePath(steamRoot) };
        string libraryFoldersFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFoldersFile))
        {
            foreach (string value in ReadQuotedValues(libraryFoldersFile, "path"))
            {
                string normalized = NormalizePath(value.Replace("\\\\", "\\"));
                if (!string.IsNullOrWhiteSpace(normalized) && !libraries.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    libraries.Add(normalized);
                }
            }
        }
        foreach (string library in libraries)
        {
            yield return library;
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
    }

    private static string? TryGetInstallDir(string library)
    {
        string manifestsDir = Path.Combine(library, "steamapps");
        foreach (string extension in new[] { ".acf", ".vdf" })
        {
            string manifest = Path.Combine(manifestsDir, $"appmanifest_{TimberbornAppId}{extension}");
            if (File.Exists(manifest))
            {
                foreach (string value in ReadQuotedValues(manifest, "installdir"))
                {
                    return value.Replace("\\\\", "\\");
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Minimal VDF reader: returns every value following the given quoted key.
    /// </summary>
    private static IEnumerable<string> ReadQuotedValues(string filePath, string key)
    {
        foreach (string rawLine in File.ReadAllLines(filePath))
        {
            string line = rawLine.Trim();
            if (!line.StartsWith("\"" + key + "\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            int valueStart = line.IndexOf('"', key.Length + 2);
            if (valueStart < 0)
            {
                continue;
            }
            int valueEnd = line.IndexOf('"', valueStart + 1);
            if (valueEnd < 0)
            {
                continue;
            }
            yield return line.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }
    }
}



