using System;
using Microsoft.Win32;

namespace TimberbornLauncher;

/// <summary>
/// Reads and writes the same enabled/priority state the game stores via Unity PlayerPrefs.
/// Location: HKCU\Software\Mechanistry\Timberborn
/// Unity appends a djb2-xor hash suffix to each key, replicated here so the game
/// sees exactly the values the launcher writes.
/// </summary>
public static class ModPlayerPrefs
{
    private const string RegistryPath = @"Software\Mechanistry\Timberborn";

    private const string ModEnabledPrefix = "ModEnabled";

    private const string ModPriorityPrefix = "ModPriority";

    public static bool IsModEnabled(string displaySource, string originName, string id)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key == null)
        {
            return true;
        }
        object? value = key.GetValue(GetKey(ModEnabledPrefix, displaySource, originName, id));
        return value == null || Convert.ToInt32(value) == 1;
    }

    public static void SetModEnabled(string displaySource, string originName, string id, bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue(GetKey(ModEnabledPrefix, displaySource, originName, id), enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    public static int GetModPriority(string displaySource, string originName, string id)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key == null)
        {
            return 0;
        }
        object? value = key.GetValue(GetKey(ModPriorityPrefix, displaySource, originName, id));
        return value == null ? 0 : Convert.ToInt32(value);
    }

    public static void SetModPriority(string displaySource, string originName, string id, int priority)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue(GetKey(ModPriorityPrefix, displaySource, originName, id), priority, RegistryValueKind.DWord);
    }

    public static void ResetModPriorities()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        if (key == null)
        {
            return;
        }
        foreach (string name in key.GetValueNames())
        {
            if (name.StartsWith(ModPriorityPrefix + ".", StringComparison.Ordinal))
            {
                key.DeleteValue(name, throwOnMissingValue: false);
            }
        }
    }

    private static string GetKey(string prefix, string displaySource, string originName, string id)
    {
        string raw = $"{prefix}.{displaySource}.{originName}.{id}";
        return raw + "_h" + Hash(raw);
    }

    /// <summary>Full PlayerPrefs key name for the mod's enabled state.</summary>
    public static string GetModEnabledKey(string displaySource, string originName, string id)
    {
        return GetKey(ModEnabledPrefix, displaySource, originName, id);
    }

    /// <summary>Full PlayerPrefs key name for the mod's priority.</summary>
    public static string GetModPriorityKey(string displaySource, string originName, string id)
    {
        return GetKey(ModPriorityPrefix, displaySource, originName, id);
    }

    private static uint Hash(string name)
    {
        uint hash = 5381;
        foreach (char c in name)
        {
            hash = hash * 33 ^ (uint)c;
        }
        return hash;
    }
}



