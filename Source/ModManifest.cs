using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TimberbornLauncher.Mods;

/// <summary>
/// Subset of the fields Timberborn's manifest.json defines.
/// </summary>
public sealed class ModManifest
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Version { get; set; } = "";

    public string Description { get; set; } = "";

    public string MinimumGameVersion { get; set; } = "";

    public List<ModDependency> RequiredMods { get; set; } = new();

    public List<ModDependency> OptionalMods { get; set; } = new();

    /// <summary>
    /// Parses a manifest.json the same way the game does (Newtonsoft JObject.Parse —
    /// lenient on trailing commas and raw newlines) while rejecting what the game rejects:
    /// missing or non-string Id/Name/Version/MinimumGameVersion, invalid version strings,
    /// non-array dependency lists, and dependency entries without an Id. Any failure is
    /// logged and yields null — never throws.
    /// </summary>
    public static ModManifest? TryReadFile(string manifestPath)
    {
        try
        {
            string json = File.ReadAllText(manifestPath);
            JObject root = JObject.Parse(json.Replace("\":-.,", "\":0.0,"));
            string version = GetRequiredString(root, "Version", manifestPath);
            string minimumGameVersion = GetRequiredString(root, "MinimumGameVersion", manifestPath);
            if (!IsValidGameVersion(version) || !IsValidGameVersion(minimumGameVersion))
            {
                throw new InvalidDataException($"Invalid game version in manifest: Version='{version}', MinimumGameVersion='{minimumGameVersion}'");
            }
            return new ModManifest
            {
                Id = GetRequiredString(root, "Id", manifestPath),
                Name = GetRequiredString(root, "Name", manifestPath),
                Version = version,
                MinimumGameVersion = minimumGameVersion,
                Description = root.ContainsKey("Description") ? GetRequiredString(root, "Description", manifestPath) : "",
                RequiredMods = ReadVersionedMods(root, "RequiredMods", manifestPath),
                OptionalMods = ReadVersionedMods(root, "OptionalMods", manifestPath),
            };
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load mod manifest from {manifestPath}", ex);
            return null;
        }
    }

    /// <summary>
    /// Rejects on the same rules as the game's Version.Create (Timberborn.Versioning.cs):
    /// empty strings throw ('v' legacy format is treated as 0.0.0.0), otherwise the part
    /// before the first '-' must be dot-separated integers.
    /// </summary>
    private static bool IsValidGameVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }
        if (version[0] == 'v')
        {
            return true;
        }
        string numeric = version.Split('-')[0];
        foreach (string part in numeric.Split('.'))
        {
            if (!int.TryParse(part, out _))
            {
                return false;
            }
        }
        return true;
    }

    private static string GetRequiredString(JObject root, string key, string manifestPath)
    {
        if (root[key] is not JValue value || value.Type != JTokenType.String)
        {
            throw new InvalidDataException($"Manifest field '{key}' is missing or not a string ({manifestPath})");
        }
        return value.Value<string>()!;
    }

    /// <summary>
    /// Mirrors the game's GetVersionedMods (Timberborn.Modding.cs): optional array of objects,
    /// each requiring an Id; MinimumVersion is optional and defaults to "0".
    /// </summary>
    private static List<ModDependency> ReadVersionedMods(JObject root, string arrayName, string manifestPath)
    {
        var result = new List<ModDependency>();
        if (!root.ContainsKey(arrayName))
        {
            return result;
        }
        if (root[arrayName] is not JArray array)
        {
            throw new InvalidDataException($"Manifest field '{arrayName}' is not an array ({manifestPath})");
        }
        foreach (JToken token in array)
        {
            if (token is not JObject dependencyObject)
            {
                throw new InvalidDataException($"Dependency entry in '{arrayName}' is not an object ({manifestPath})");
            }
            string id = GetRequiredString(dependencyObject, "Id", manifestPath);
            string minimumVersion;
            if (dependencyObject.ContainsKey("MinimumVersion"))
            {
                minimumVersion = GetRequiredString(dependencyObject, "MinimumVersion", manifestPath);
            }
            else
            {
                minimumVersion = "0";
            }
            if (!IsValidGameVersion(minimumVersion))
            {
                throw new InvalidDataException($"Dependency '{id}' has invalid MinimumVersion '{minimumVersion}' ({manifestPath})");
            }
            result.Add(new ModDependency
            {
                Id = id,
                MinimumVersionRaw = minimumVersion,
            });
        }
        return result;
    }
}

public sealed class ModDependency
{
    public string Id { get; set; } = "";

    public string MinimumVersionRaw { get; set; } = "";

    public string MinimumVersion => string.IsNullOrEmpty(MinimumVersionRaw) ? "0" : MinimumVersionRaw;
}
