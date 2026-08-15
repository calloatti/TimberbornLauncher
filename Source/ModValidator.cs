using System;
using System.Collections.Generic;
using System.Linq;

namespace TimberbornLauncher;

/// <summary>
/// Validates the enabled mod set against the database before it is pushed to the registry
/// for a game run. Blocking warnings abort Run Game: the game crashes when two enabled
/// mods share an id, a required dependency that is missing or disabled keeps the
/// dependent mod from loading, and cycles in user load order make the order ambiguous.
/// </summary>
public sealed class ModWarning
{
    public string Id { get; init; } = "";
    public string Message { get; init; } = "";
    public string SeverityLabel => "Error";
}

public static class ModValidator
{
    /// <summary>
    /// Recomputes the warnings from the enabled mod set directly in the database,
    /// replaces the contents of the `warnings` table, and returns how many exist.
    /// </summary>
    public static int RefreshWarnings()
    {
        var warnings = new List<ModWarning>();

        foreach (var row in AppDatabase.GetDuplicateEnabledModIds())
        {
            warnings.Add(new ModWarning
            {
                Id = row.ModId,
                Message = $"Duplicate enabled mod id \"{row.ModId}\": {row.Members}."
            });
        }

        foreach (var row in AppDatabase.GetMissingRequiredDependencies())
        {
            warnings.Add(new ModWarning
            {
                Id = row.ModId,
                Message = $"\"{row.ModId}\" requires \"{row.DependencyId}\", which is missing or disabled."
            });
        }

        // *** NEW: cycle detection for user load order ***
        foreach (string cycle in AppDatabase.GetUserDependencyCycles())
        {
            warnings.Add(new ModWarning
            {
                Id = "cycle",
                Message = $"Circular dependency in user load order: {cycle}. Please remove or adjust one of the rules."
            });
        }

        // Conflict rules where both mods are enabled
        foreach (var (mod1Id, mod2Id) in AppDatabase.GetEnabledConflicts())
        {
            warnings.Add(new ModWarning
            {
                Id = mod1Id,
                Message = $"\"{mod1Id}\" conflicts with \"{mod2Id}\", and both are enabled. Disable one to continue."
            });
        }

        AppDatabase.InTransaction(() =>
        {
            AppDatabase.ClearWarnings();
            foreach (ModWarning warning in warnings.OrderBy(w => w.Message, StringComparer.CurrentCulture))
            {
                AppDatabase.InsertWarning(true, warning.Message);
            }
        });

        Log.Info($"Mod validator: {warnings.Count} warning(s)");
        if (MainForm.WarningsStatusLabelInstance != null)
        {
            MainForm.WarningsStatusLabelInstance.Text = warnings.Count + " Warnings";
            MainForm.WarningsStatusLabelInstance.ForeColor = warnings.Count > 0 ? System.Drawing.Color.Firebrick : SystemColors.ControlText;
        }
        return warnings.Count;
    }
}


