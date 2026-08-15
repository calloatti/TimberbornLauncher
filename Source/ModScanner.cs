using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimberbornLauncher.Versioning;

namespace TimberbornLauncher.Mods;

/// <summary>
/// Scans every mod manifest.json (mod root AND each version-* subfolder) into the
/// session table, mirroring the game's discovery, then marks the selected row the way
/// the game would (best version-* folder for the installed game version; no match =
/// nothing selected). PlayerPrefs keys/values are version-agnostic (registry keys carry
/// source + origin + mod id, never version), so they're read once per unique mod and
/// written to every manifest row; dependency rows are written only for the selected one.
/// </summary>
public static class ModScanner
{
    private const string ManifestFileName = "manifest.json";

    private const string VersionFolderPrefix = "version-";

    private sealed class ModRoot
    {
        public string Path { get; init; } = "";

        public string Source { get; init; } = "local";

        public string OriginName { get; init; } = "";
    }

    public static void Scan()
    {
        GameVersion? currentVersion = GameVersionReader.TryReadCurrentVersion();
        var entries = new List<ModRootEntry>();
        var folderCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int discarded = 0;
        AppDatabase.InTransaction(() =>
        {
            AppDatabase.ClearSessionTables();

            foreach (ModRoot root in EnumerateModRoots())
            {
                folderCounts.TryGetValue(root.Source, out int count);
                folderCounts[root.Source] = count + 1;
                (List<ModRootEntry> rootEntries, int rootDiscarded) = CollectManifests(root);
                entries.AddRange(rootEntries);
                discarded += rootDiscarded;
            }

            MarkSelections(entries, currentVersion);
            EnrichRegistry(entries);
        });

        string folderSummary = folderCounts.Count == 0
            ? "none"
            : string.Join(", ", folderCounts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}: {kv.Value}"));
        Log.Info($"Mod scan: top-level folders — {folderSummary}");

        var selectedGroups = entries.Where(entry => entry.Selected).GroupBy(entry => entry.Root.Source).ToList();
        string selectedSummary = selectedGroups.Count == 0
            ? "none"
            : string.Join(", ", selectedGroups.Select(g => $"{g.Key}: {g.Count()}"));
        Log.Info($"Mod scan: selected mods — {selectedSummary}");

        Log.Info($"Mod scan: discarded manifest.json files: {discarded}");
    }

    private sealed class ModRootEntry
    {
        public ModRoot Root { get; init; } = new();

        public string ManifestPath { get; init; } = "";

        public string FolderPath { get; init; } = "";

        public string VersionFolder { get; init; } = "";

        public ModManifest Manifest { get; init; } = new();

        public bool Selected { get; set; }
    }

    private static (List<ModRootEntry> Entries, int Discarded) CollectManifests(ModRoot root)
    {
        var entries = new List<ModRootEntry>();
        int discarded = 0;
        string rootManifest = Path.Combine(root.Path, ManifestFileName);
        if (File.Exists(rootManifest))
        {
            ModManifest? manifest = TryReadManifest(rootManifest);
            if (manifest != null && !string.IsNullOrWhiteSpace(manifest.Id))
            {
                entries.Add(new ModRootEntry
                {
                    Root = root,
                    ManifestPath = rootManifest,
                    FolderPath = root.Path,
                    VersionFolder = "",
                    Manifest = manifest
                });
            }
            else
            {
                if (manifest != null)
                {
                    Log.Error($"Mod id from manifest \"{rootManifest}\" is empty");
                }
                discarded++;
            }
        }
        foreach (string folder in Directory.GetDirectories(root.Path, VersionFolderPrefix + "*"))
        {
            string manifestPath = Path.Combine(folder, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                continue;
            }
            ModManifest? manifest = TryReadManifest(manifestPath);
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
            {
                if (manifest != null)
                {
                    Log.Error($"Mod id from manifest \"{manifestPath}\" is empty");
                }
                discarded++;
                continue;
            }
            entries.Add(new ModRootEntry
            {
                Root = root,
                ManifestPath = manifestPath,
                FolderPath = folder,
                VersionFolder = Path.GetFileName(folder),
                Manifest = manifest
            });
        }
        foreach (ModRootEntry entry in entries)
        {
            AppDatabase.InsertScannedMod(
                entry.ManifestPath, root.Source, root.OriginName, entry.VersionFolder,
                entry.Manifest.Id, entry.Manifest.Name, entry.Manifest.Version, entry.Manifest.Description, entry.Manifest.MinimumGameVersion,
                selected: 0, "", "", 1, 0);
        }
        return (entries, discarded);
    }

    private static void MarkSelections(List<ModRootEntry> entries, GameVersion? currentVersion)
    {
        foreach (IGrouping<string, ModRootEntry> group in entries.GroupBy(entry => entry.Root.Path))
        {
            var versionFolders = group
                .Where(entry => entry.VersionFolder != "")
                .Select(entry => entry.FolderPath)
                .ToList();
            ModRootEntry? selectedEntry = null;
            if (versionFolders.Count > 0)
            {
                string? selectedManifest = SelectBestVersionManifest(group.Key, versionFolders, currentVersion);
                selectedEntry = group.FirstOrDefault(entry => entry.ManifestPath == selectedManifest);
            }
            else
            {
                selectedEntry = group.FirstOrDefault(entry => entry.VersionFolder == "");
            }
            if (selectedEntry == null)
            {
                continue;
            }
            selectedEntry.Selected = true;
            AppDatabase.MarkSelected(selectedEntry.ManifestPath);
        }
    }

    private static void EnrichRegistry(List<ModRootEntry> entries)
    {
        var cache = new Dictionary<(string DisplaySource, string OriginName, string Id), RegistryState>();
        foreach (ModRootEntry entry in entries)
        {
            string displaySource = entry.Root.Source == "local" ? "Local" : "Steam Workshop";
            RegistryState state = GetRegistryState(cache, displaySource, entry.Root.OriginName, entry.Manifest.Id);
            AppDatabase.UpdateScannedModValues(entry.ManifestPath, state.EnabledKey, state.PriorityKey, state.EnabledValue, 0);

            if (!entry.Selected)
            {
                continue;
            }

            foreach (ModDependency dependency in entry.Manifest.RequiredMods)
            {
                AppDatabase.InsertModDependency(entry.ManifestPath, entry.Manifest.Id, "required", dependency.Id, dependency.MinimumVersion);
            }
            foreach (ModDependency dependency in entry.Manifest.OptionalMods)
            {
                AppDatabase.InsertModDependency(entry.ManifestPath, entry.Manifest.Id, "optional", dependency.Id, dependency.MinimumVersion);
            }
        }
    }

    private static RegistryState GetRegistryState(Dictionary<(string DisplaySource, string OriginName, string Id), RegistryState> cache, string displaySource, string originName, string id)
    {
        if (cache.TryGetValue((displaySource, originName, id), out RegistryState? existing))
        {
            return existing;
        }
        var state = new RegistryState(
            ModPlayerPrefs.GetModEnabledKey(displaySource, originName, id),
            ModPlayerPrefs.GetModPriorityKey(displaySource, originName, id),
            ModPlayerPrefs.IsModEnabled(displaySource, originName, id) ? 1 : 0);
        cache[(displaySource, originName, id)] = state;
        return state;
    }

    private sealed record RegistryState(string EnabledKey, string PriorityKey, int EnabledValue);

    /// <summary>
    /// Picks the highest version-* subfolder the installed game version is
    /// equal-or-higher than; null when none matches (the game omits the mod).
    /// </summary>
    private static string? SelectBestVersionManifest(string modRoot, List<string> versionFolders, GameVersion? currentVersion)
    {
        var candidates = new List<(string Folder, string Manifest, GameVersion Version)>();
        foreach (string folder in versionFolders)
        {
            string versionText = Path.GetFileName(folder).Substring(VersionFolderPrefix.Length);
            GameVersion? folderVersion = string.IsNullOrEmpty(versionText)
                ? currentVersion ?? GameVersion.TryCreate("0.0.0.0")
                : GameVersion.TryCreate(versionText);
            if (folderVersion == null)
            {
                continue;
            }
            string manifestPath = Path.Combine(folder, ManifestFileName);
            if (File.Exists(manifestPath))
            {
                candidates.Add((folder, manifestPath, folderVersion));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }
        if (currentVersion == null)
        {
            return candidates.OrderByDescending(c => c.Version).First().Manifest;
        }

        var matches = candidates
            .Where(c => currentVersion.IsDevelopmentVersion || currentVersion.IsEqualOrHigherThan(c.Version))
            .ToList();
        if (matches.Count == 0)
        {
            return null;
        }
        return matches.OrderByDescending(c => c.Version).First().Manifest;
    }

    private static IEnumerable<ModRoot> EnumerateModRoots()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string localModsRoot = Path.Combine(documents, "Timberborn", "Mods");
        if (Directory.Exists(localModsRoot))
        {
            foreach (string dir in Directory.GetDirectories(localModsRoot))
            {
                yield return new ModRoot { Path = dir, Source = "local", OriginName = Path.GetFileName(dir) };
            }
        }

        var seenWorkshopIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string workshopRoot in GameLocator.GetWorkshopContentRoots())
        {
            foreach (string dir in Directory.GetDirectories(workshopRoot))
            {
                string itemId = Path.GetFileName(dir);
                if (!seenWorkshopIds.Add(itemId))
                {
                    continue;
                }
                yield return new ModRoot { Path = dir, Source = "steam", OriginName = itemId };
            }
        }
    }

    private static ModManifest? TryReadManifest(string manifestPath)
    {
        return ModManifest.TryReadFile(manifestPath);
    }
}


