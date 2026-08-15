using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TimberbornLauncher;

namespace TimberbornLauncher.Mods;

public static class ModSorter
{
  /// <summary>Sentinel start value guarantees the first ComputeLoadOrder runs even when
  /// app_state has no row (no user-dependency edit yet) or the value is null.</summary>
  private static string? _lastComputedUserDepsTimestamp = "__never_computed__";

  private sealed class ModRow
  {
    public string ModPath { get; init; } = "";
    public string Source { get; init; } = "";
    public string OriginName { get; init; } = "";
    public string ModId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public List<string> Dependencies { get; } = new();
  }

  public static bool Apply(Form owner)
  {
    Log.Info("Apply: starting");
    try
    {
      Log.Info("Apply: calling ModValidator.RefreshWarnings");
      ModValidator.RefreshWarnings();
      if (AppDatabase.GetBlockingWarningCount() > 0)
      {
        Log.Info($"Apply: blocking warnings found ({AppDatabase.GetBlockingWarningCount()})");
        if (owner is MainForm mainForm)
        {
          mainForm.ShowWarningsView();
        }
        return false;
      }

      Log.Info("Apply: calling ComputeLoadOrder");
      ComputeLoadOrder();

      Log.Info("Apply: pushing state to registry");
      ModPlayerPrefs.ResetModPriorities();
      foreach (ModEntry mod in AppDatabase.GetModList())
      {
        bool enabled = mod.EnabledValue == 1;
        ModPlayerPrefs.SetModEnabled(mod.DisplaySource, mod.OriginName, mod.Id, enabled);
        ModPlayerPrefs.SetModPriority(mod.DisplaySource, mod.OriginName, mod.Id, mod.PriorityValue);
      }
      Log.Info("Apply: completed successfully");
      return true;
    }
    catch (Exception ex)
    {
      Log.Error("Apply: failed", ex);
      MessageBox.Show(owner, "Failed to write mod settings:\n" + ex.Message, "Timberborn Launcher",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }
  }

  public static void ComputeLoadOrder()
  {
    string? dbValue = AppDatabase.GetAppStateValue("user_dependencies_last_modified");
    if (_lastComputedUserDepsTimestamp == dbValue)
    {
      return;
    }

    Log.Info("Mod sort: computing load order");
    const int priorityBase = 2000000000;
    const int priorityStep = 100000;

    DataTable modsTable = AppDatabase.ExecuteQuery(
        """
            SELECT mod_path, source, origin_name, mod_id, name
            FROM mods
            WHERE selected = 1;
            """);

    // Duplicate Ids get OriginName/ prefix on DisplayName (mirrors the game).
    var duplicateKeys = modsTable.Rows.Cast<DataRow>()
        .GroupBy(row => (Source: row.Field<string>("source"), Id: row.Field<string>("mod_id")))
        .Where(group => group.Count() > 1)
        .Select(group => group.Key)
        .ToHashSet();

    // ---- CHANGED: store all mods, not just the first per ModId ----
    var allMods = new List<ModRow>();
    foreach (DataRow row in modsTable.Rows)
    {
      string source = row.Field<string>("source")!;
      string modId = row.Field<string>("mod_id")!;
      string originName = row.Field<string>("origin_name")!;
      string name = row.Field<string>("name")!;
      allMods.Add(new ModRow
      {
        ModPath = row.Field<string>("mod_path")!,
        Source = source,
        OriginName = originName,
        ModId = modId,
        DisplayName = duplicateKeys.Contains((source, modId)) ? $"{originName}/{name}" : name
      });
    }

    // Build a lookup from ModId to the list of ModRow objects that have that id.
    var modsById = allMods.GroupBy(m => m.ModId).ToDictionary(g => g.Key, g => g.ToList());

    // Dependencies: add to all mods that have the dependency ModId.
    DataTable depsTable = AppDatabase.ExecuteQuery(
        """
            SELECT d.mod_id, d.dependency_id
            FROM mod_dependencies d
            JOIN mods m ON m.mod_path = d.mod_path
            WHERE m.selected = 1;
            """);
    foreach (DataRow row in depsTable.Rows)
    {
      string modId = row.Field<string>("mod_id")!;
      string depId = row.Field<string>("dependency_id")!;
      if (modsById.TryGetValue(modId, out List<ModRow>? rows))
      {
        foreach (ModRow mod in rows)
        {
          mod.Dependencies.Add(depId);
        }
      }
    }

    DataTable userDepsTable = AppDatabase.ExecuteQuery(
        """
            SELECT mod_id, dependency_id
            FROM user_dependencies
            WHERE dependency_type != 'conflicts';
            """);
    foreach (DataRow row in userDepsTable.Rows)
    {
      string modId = row.Field<string>("mod_id")!;
      string depId = row.Field<string>("dependency_id")!;
      if (modsById.TryGetValue(modId, out List<ModRow>? rows))
      {
        foreach (ModRow mod in rows)
        {
          mod.Dependencies.Add(depId);
        }
      }
    }

    // Sort using the original algorithm.
    List<ModRow> ordered = SortByDependencies(
        allMods.OrderBy(mod => mod.DisplayName, StringComparer.CurrentCulture));

    // ---- CHANGED: write priority using mod_path ----
    int rank = 0;
    foreach (ModRow mod in ordered)
    {
      int priority = priorityBase - priorityStep * rank;
      AppDatabase.SetModPriorityByPath(mod.ModPath, priority);
      rank++;
    }
    _lastComputedUserDepsTimestamp = dbValue;
    Log.Info($"Mod sort: wrote priorities for {ordered.Count} mods");
  }

  private static List<ModRow> SortByDependencies(IEnumerable<ModRow> mods)
  {
    var dependencies = mods.ToDictionary(mod => mod, mod => mod.Dependencies.ToList());
    var result = new List<ModRow>();
    while (dependencies.Count > 0)
    {
      int minDependenciesCount = dependencies.Min(entry => entry.Value.Count);
      ModRow currentMod = dependencies.First(entry => entry.Value.Count == minDependenciesCount).Key;
      dependencies.Remove(currentMod);
      foreach (var entry in dependencies)
        entry.Value.RemoveAll(id => id == currentMod.ModId);
      result.Add(currentMod);
    }
    return result;
  }
}