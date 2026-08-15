using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace TimberbornLauncher;

/// <summary>
/// Single on-disk SQLite database for everything: session/scan tables plus
/// persistent profiles and user_dependencies. File = exe name + ".db", next to the exe.
/// Session tables are truncated at every launch (they're rebuilt from disk).
/// </summary>
public static class AppDatabase
{
    private static SqliteConnection? _connection;

    public const string CharEnabled = "☒";
    public const string CharDisabled = "☐";

    public static string DatabasePath =>
        Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName) + ".db");

    /// <summary>
    /// Opens the DB (creating the file) and ensures the full schema exists.
    /// Call once at startup.
    /// </summary>
    public static void Initialize()
    {
        string path = DatabasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();
        CreateSchema();
    }

    public static void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        command.ExecuteNonQuery();
    }

    public static DataTable ExecuteQuery(string sql)
    {
        var table = new DataTable();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText = sql;
        using SqliteDataReader reader = command.ExecuteReader();
        table.Load(reader);
        return table;
    }

    /// <summary>
    /// Selected mods for the grids. Same columns across all mod views; orderBy is
    /// interpolated verbatim (callers pass fixed literals only): "name" or "priority_value DESC".
    /// </summary>
    public static DataTable GetModsGridTable(string orderBy)
    {
        return ExecuteQuery($"""
            SELECT CASE WHEN enabled_value = 1 THEN '{CharEnabled}' ELSE '{CharDisabled}' END AS Enabled,
                   source AS Source, mod_path, name AS Name, version AS Version, mod_id AS Id, origin_name AS OriginName, version_folder AS Folder
            FROM mods
            WHERE selected = 1
            ORDER BY {orderBy};
            """);
    }

    /// <summary>Reads a single value from the app_state KV table; null when absent.</summary>
    public static string? GetAppStateValue(string key)
    {
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText = "SELECT value FROM app_state WHERE key = @key;";
        command.Parameters.AddWithValue("@key", key);
        return command.ExecuteScalar() as string;
    }

    /// <summary>Runs a block of work inside a transaction.</summary>
    public static void InTransaction(Action action)
    {
        using SqliteTransaction transaction = _connection!.BeginTransaction();
        try
        {
            action();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void CreateSchema()
    {
        Execute(
            """
            DROP TABLE IF EXISTS mod_dependencies;
            DROP TABLE IF EXISTS mods;
            DROP TABLE IF EXISTS warnings;

            CREATE TABLE IF NOT EXISTS mods (
                mod_path TEXT PRIMARY KEY,
                source TEXT NOT NULL,
                origin_name TEXT NOT NULL,
                version_folder TEXT NOT NULL,
                mod_id TEXT NOT NULL,
                name TEXT NOT NULL,
                version TEXT NOT NULL,
                description TEXT NOT NULL,
                minimum_game_version TEXT NOT NULL,
                selected INTEGER NOT NULL DEFAULT 0,
                enabled_registry_key TEXT NOT NULL DEFAULT '',
                priority_registry_key TEXT NOT NULL DEFAULT '',
                enabled_value INTEGER NOT NULL DEFAULT 1,
                priority_value INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS mod_dependencies (
                mod_path TEXT NOT NULL REFERENCES mods(mod_path),
                mod_id TEXT NOT NULL,
                dependency_type TEXT NOT NULL,
                dependency_id TEXT NOT NULL,
                minimum_version TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS warnings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                is_blocking INTEGER NOT NULL,
                message TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS profiles (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                date_created TEXT NOT NULL,
                name TEXT NOT NULL,
                description TEXT NOT NULL,
                game_version TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS profile_mods (
                profile_id INTEGER NOT NULL REFERENCES profiles(id),
                position INTEGER NOT NULL,
                mod_id TEXT NOT NULL,
                source TEXT NOT NULL,
                mod_version TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS user_dependencies (
                hash TEXT PRIMARY KEY,
                mod_id TEXT NOT NULL,
                dependency_type TEXT NOT NULL,
                dependency_id TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS app_state (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TRIGGER IF NOT EXISTS user_dependencies_last_modified_insert
            AFTER INSERT ON user_dependencies
            BEGIN
                INSERT INTO app_state (key, value)
                VALUES ('user_dependencies_last_modified', strftime('%Y-%m-%d %H:%M:%f','now'))
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            END;

            CREATE TRIGGER IF NOT EXISTS user_dependencies_last_modified_update
            AFTER UPDATE ON user_dependencies
            BEGIN
                INSERT INTO app_state (key, value)
                VALUES ('user_dependencies_last_modified', strftime('%Y-%m-%d %H:%M:%f','now'))
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            END;

            CREATE TRIGGER IF NOT EXISTS user_dependencies_last_modified_delete
            AFTER DELETE ON user_dependencies
            BEGIN
                INSERT INTO app_state (key, value)
                VALUES ('user_dependencies_last_modified', strftime('%Y-%m-%d %H:%M:%f','now'))
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            END;
            """);
    }

    /// <summary>
    /// Session tables carry transient scan data; truncate at every launch.
    /// </summary>
    public static void ClearSessionTables()
    {
        Execute("DELETE FROM mod_dependencies;");
        Execute("DELETE FROM mods;");
        Execute("DELETE FROM warnings;");
    }

    public static void InsertScannedMod(string modPath, string source, string originName,
        string versionFolder, string modId, string name, string version, string description, string minimumGameVersion,
        int selected, string enabledRegistryKey, string priorityRegistryKey, int enabledValue, int priorityValue)
    {
        Execute(
            """
            INSERT INTO mods
                (mod_path, source, origin_name, version_folder, mod_id, name, version, description,
                 minimum_game_version, selected, enabled_registry_key, priority_registry_key, enabled_value, priority_value)
            VALUES
                (@modPath, @source, @originName, @versionFolder, @modId, @name, @version, @description,
                 @minimumGameVersion, @selected, @enabledRegistryKey, @priorityRegistryKey, @enabledValue, @priorityValue);
            """,
            ("@modPath", modPath), ("@source", source), ("@originName", originName), ("@versionFolder", versionFolder),
            ("@modId", modId), ("@name", name), ("@version", version), ("@description", description),
            ("@minimumGameVersion", minimumGameVersion), ("@selected", selected),
            ("@enabledRegistryKey", enabledRegistryKey), ("@priorityRegistryKey", priorityRegistryKey),
            ("@enabledValue", enabledValue), ("@priorityValue", priorityValue));
    }

    public static void InsertModDependency(string modPath, string modId, string dependencyType, string dependencyId, string minimumVersion)
    {
        Execute(
            """
            INSERT INTO mod_dependencies (mod_path, mod_id, dependency_type, dependency_id, minimum_version)
            VALUES (@modPath, @modId, @dependencyType, @dependencyId, @minimumVersion);
            """,
            ("@modPath", modPath), ("@modId", modId), ("@dependencyType", dependencyType),
            ("@dependencyId", dependencyId), ("@minimumVersion", minimumVersion));
    }

    public static void MarkSelected(string modPath)
    {
        Execute(
            """
            UPDATE mods SET selected = 1 WHERE mod_path = @modPath;
            """,
            ("@modPath", modPath));
    }

    public static void UpdateScannedModValues(string modPath, string enabledRegistryKey, string priorityRegistryKey, int enabledValue, int priorityValue)
    {
        Execute(
            """
            UPDATE mods
            SET enabled_registry_key = @enabledRegistryKey, priority_registry_key = @priorityRegistryKey,
                enabled_value = @enabledValue, priority_value = @priorityValue
            WHERE mod_path = @modPath;
            """,
            ("@modPath", modPath), ("@enabledRegistryKey", enabledRegistryKey), ("@priorityRegistryKey", priorityRegistryKey),
            ("@enabledValue", enabledValue), ("@priorityValue", priorityValue));
    }

    public static string ComputeUserDependencyKey(string modId, string dependencyType, string dependencyId)
    {
        uint hash = 5381;
        string combined = $"{modId.ToLowerInvariant()}|{dependencyType.ToLowerInvariant()}|{dependencyId.ToLowerInvariant()}";
        foreach (char c in combined)
        {
            hash = hash * 33 ^ (uint)c;
        }
        return hash.ToString("x8");
    }

    public static string InsertUserDependency(string modId, string dependencyType, string dependencyId)
    {
        if (modId == dependencyId)
        {
            MessageBox.Show("Cannot add a dependency on itself.", "Invalid Dependency", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "";
        }

        string invertedHash = ComputeUserDependencyKey(dependencyId, dependencyType, modId);
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM user_dependencies WHERE hash = @h;";
        cmd.Parameters.AddWithValue("@h", invertedHash);
        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
        {
            MessageBox.Show("Adding this dependency would create a circular dependency.", "Circular Dependency", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "";
        }

        string hash = ComputeUserDependencyKey(modId, dependencyType, dependencyId);
        Execute(
            """
            INSERT OR REPLACE INTO user_dependencies (hash, mod_id, dependency_type, dependency_id)
            VALUES (@hash, @modId, @depType, @depId);
            """,
            ("@hash", hash), ("@modId", modId), ("@depType", dependencyType), ("@depId", dependencyId));
        return hash;
    }

    public static void DeleteUserDependency(string hash)
    {
        Execute("DELETE FROM user_dependencies WHERE hash = @hash;", ("@hash", hash));
    }

    /// <summary>
    /// Adds a conflict rule (mod1, 'conflicts', mod2). Conflict is symmetric, so the same
    /// pair in either direction is treated as a duplicate and rejected.
    /// </summary>
    public static string InsertUserConflict(string modId, string conflictId)
    {
        if (modId == conflictId)
        {
            MessageBox.Show("Cannot add a conflict with itself.", "Invalid Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "";
        }

        string forward = ComputeUserDependencyKey(modId, "conflicts", conflictId);
        string backward = ComputeUserDependencyKey(conflictId, "conflicts", modId);
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM user_dependencies WHERE hash IN (@f, @b);";
        cmd.Parameters.AddWithValue("@f", forward);
        cmd.Parameters.AddWithValue("@b", backward);
        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
        {
            MessageBox.Show("These mods already have a conflict rule.", "Duplicate Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return "";
        }

        Execute(
            """
            INSERT INTO user_dependencies (hash, mod_id, dependency_type, dependency_id)
            VALUES (@hash, @modId, 'conflicts', @conflictId);
            """,
            ("@hash", forward), ("@modId", modId), ("@conflictId", conflictId));
        return forward;
    }

    /// <summary>
    /// Conflict rules whose both mods are currently enabled. Blocking: the game loads both.
    /// </summary>
    public static List<(string Mod1Id, string Mod2Id)> GetEnabledConflicts()
    {
        var result = new List<(string, string)>();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText =
            """
            SELECT d.mod_id, d.dependency_id
            FROM user_dependencies d
            WHERE d.dependency_type = 'conflicts'
              AND d.mod_id IN (SELECT mod_id FROM mods WHERE selected = 1 AND enabled_value = 1)
              AND d.dependency_id IN (SELECT mod_id FROM mods WHERE selected = 1 AND enabled_value = 1)
            ORDER BY d.mod_id;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add((reader.GetString(0), reader.GetString(1)));
        }
        return result;
    }

    /// <summary>
    /// Detects cycles in the user-defined load-order graph.
    /// Returns a list of cycle descriptions (e.g., "A → B → C → A").
    /// user_dependencies rows mean the same as mod_dependencies rows (mod_id depends on
    /// dependency_id), so the edge is mod_id → dependency_id. Conflict rows are not
    /// ordering edges and are excluded.
    /// </summary>
    public static List<string> GetUserDependencyCycles()
    {
        var edges = new List<(string From, string To)>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT mod_id, dependency_id FROM user_dependencies WHERE dependency_type != 'conflicts';";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            edges.Add((reader.GetString(0), reader.GetString(1)));

        // Build adjacency list
        var graph = new Dictionary<string, List<string>>();
        foreach (var (from, to) in edges)
        {
            if (!graph.ContainsKey(from))
                graph[from] = new List<string>();
            graph[from].Add(to);
        }

        var cycles = new List<string>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();

        void Dfs(string node)
        {
            if (recursionStack.Contains(node))
            {
                // Found a cycle: extract from path
                int startIdx = path.IndexOf(node);
                if (startIdx >= 0)
                {
                    var cycle = path.Skip(startIdx).Concat(new[] { node }).ToList();
                    cycles.Add(string.Join(" → ", cycle));
                }
                return;
            }
            if (visited.Contains(node)) return;

            visited.Add(node);
            recursionStack.Add(node);
            path.Add(node);

            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (string neighbor in neighbors)
                    Dfs(neighbor);
            }

            path.RemoveAt(path.Count - 1);
            recursionStack.Remove(node);
        }

        foreach (string node in graph.Keys)
            Dfs(node);

        return cycles;
    }

    /// <summary>
    /// Toggle writes land here, in the session DB (never the registry). Applied to every
    /// manifest row of the mod, matching the version-agnostic registry semantics.
    /// </summary>
    public static void SetModEnabledByPath(string modPath, int enabled)
    {
        Execute(
            """
            UPDATE mods SET enabled_value = @enabledValue
            WHERE mod_path = @modPath;
            """,
            ("@enabledValue", enabled), ("@modPath", modPath));
    }

    public static void SetModPriority(string source, string originName, string modId, int priority)
    {
        Execute(
            """
            UPDATE mods SET priority_value = @priority
            WHERE source = @source AND origin_name = @originName AND mod_id = @modId AND selected = 1;
            """,
            ("@priority", priority), ("@source", source), ("@originName", originName), ("@modId", modId));
    }

    public static void InsertWarning(bool isBlocking, string message)
    {
        Execute(
            """
            INSERT INTO warnings (is_blocking, message)
            VALUES (@isBlocking, @message);
            """,
            ("@isBlocking", isBlocking ? 1 : 0), ("@message", message));
    }

    public static void ClearWarnings()
    {
        Execute("DELETE FROM warnings;");
    }

    public static List<ModWarning> GetWarningList()
    {
        var result = new List<ModWarning>();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText = "SELECT message FROM warnings ORDER BY message;";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ModWarning
            {
                Message = reader.GetString(0)
            });
        }
        return result;
    }

  public static SqliteDataReader ExecuteReader(string sql)
  {
    SqliteCommand command = _connection!.CreateCommand();
    command.CommandText = sql;
    return command.ExecuteReader();
  }
  
  public static int GetBlockingWarningCount()
    {
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM warnings WHERE is_blocking = 1;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Selected mods as displayed in the human-order grid, straight from the session table.
    /// Duplicate-Id marking mirrors the game (Id + source type).
    /// </summary>
    public static List<ModEntry> GetModList()
    {
        var result = new List<ModEntry>();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText =
            """
            SELECT source, origin_name, mod_id, name, version, version_folder, enabled_value, priority_value
            FROM mods
            WHERE selected = 1;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ModEntry
            {
                Source = reader.GetString(0),
                OriginName = reader.GetString(1),
                Id = reader.GetString(2),
                Name = reader.GetString(3),
                Version = reader.GetString(4),
                VersionFolder = reader.GetString(5),
                EnabledValue = reader.GetInt32(6),
                PriorityValue = reader.GetInt32(7)
            });
        }
        foreach (IGrouping<(string Id, string Source), ModEntry> group in result
                     .GroupBy(entry => (entry.Id, entry.Source))
                     .Where(group => group.Count() > 1))
        {
            foreach (ModEntry entry in group)
            {
                entry.IsIdDuplicated = true;
            }
        }
        return result;
    }

  public static void SetModPriorityByPath(string modPath, int priority)
  {
    Execute(
        "UPDATE mods SET priority_value = @priority WHERE mod_path = @modPath;",
        ("@priority", priority), ("@modPath", modPath));
  }

  /// <summary>
  /// Enabled mod ids that appear more than once across sources.
  /// </summary>
  public static List<DuplicateEnabledModIdRow> GetDuplicateEnabledModIds()
    {
        var result = new List<DuplicateEnabledModIdRow>();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText =
            """
            SELECT mod_id, group_concat(origin_name || ' (' || source || ')')
            FROM mods
            WHERE selected = 1 AND enabled_value = 1
            GROUP BY mod_id
            HAVING COUNT(*) > 1
            ORDER BY mod_id;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new DuplicateEnabledModIdRow
            {
                ModId = reader.GetString(0),
                Members = reader.GetString(1)
            });
        }
        return result;
    }

    /// <summary>
    /// Enabled mods whose required dependency is not in the enabled mod_id set.
    /// </summary>
    public static List<MissingRequiredDependencyRow> GetMissingRequiredDependencies()
    {
        var result = new List<MissingRequiredDependencyRow>();
        using SqliteCommand command = _connection!.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT m.mod_id, m.origin_name, m.source, d.dependency_id
            FROM mods m
            JOIN mod_dependencies d ON d.mod_path = m.mod_path
            WHERE m.selected = 1
              AND m.enabled_value = 1
              AND d.dependency_type = 'required'
              AND d.dependency_id NOT IN (
                  SELECT mod_id FROM mods WHERE selected = 1 AND enabled_value = 1
              )
            ORDER BY m.mod_id;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new MissingRequiredDependencyRow
            {
                ModId = reader.GetString(0),
                OriginName = reader.GetString(1),
                Source = reader.GetString(2),
                DependencyId = reader.GetString(3)
            });
        }
        return result;
    }
}

/// <summary>
/// A row of the session `mods` table for a selected mod, as shown in the mod list.
/// </summary>
public sealed class ModEntry
{
    public string Source { get; init; } = "";

    public string OriginName { get; init; } = "";

    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Version { get; init; } = "";

    public string VersionFolder { get; init; } = "";

    public bool IsIdDuplicated { get; set; }

    public int EnabledValue { get; set; }

public int PriorityValue { get; set; }

    public bool IsUserMod => Source == "local";

    public string DisplaySource => IsUserMod ? "Local" : "Steam Workshop";

    public string DisplayName => IsIdDuplicated ? $"{OriginName}/{Name}" : Name;
}

public sealed class DuplicateEnabledModIdRow
{
    public string ModId { get; init; } = "";

    public string Members { get; init; } = "";
}

public sealed class MissingRequiredDependencyRow
{
    public string ModId { get; init; } = "";

    public string OriginName { get; init; } = "";

    public string Source { get; init; } = "";

    public string DependencyId { get; init; } = "";
}


