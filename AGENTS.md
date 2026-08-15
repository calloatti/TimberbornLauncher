# AGENTS.md

WinForms GUI launcher (net8.0-windows) that manages Timberborn mods and launches the game directly (skips the game's built‑in mod manager).

## Build

- `dotnet build -c Debug --nologo` (root, picks up `TimberbornLauncher.slnx`). No tests, no lint tasks.
- Do **NOT** spin up temp harness projects / `dotnet run` smoke‑test builds to verify behavior — the user explicitly rejected that. Build only; report honestly.
- Kill a stale instance first if re‑testing: `Stop-Process -Name TimberbornLauncher -Force`.

## Layout

- `Source/` — app entry (`Program.cs`), main window (`MainForm*`), launch/locator logic (`LaunchOptions`, `GameLocator`, `ModPlayerPrefs`) + SQLite layer (`AppDatabase`) + DB‑side models/order/validation (`ModEntry`, `ModSorter`, `ModValidator`, `PanelWarnings`) + the four view panels (`PanelHumanOrder`, `PanelGameLoadOrder`, `PanelUserLoadOrder`, `PanelWarnings` — all `UserControl`s, not Forms), custom control (`SearchTextBox`), grid height snapping (`GridIntegralHeight`), logger (`Log`). `ModSorter.Apply` owns the whole load‑order push (reads session DB, writes PlayerPrefs on Run Game/Save). `ModSave` is launch‑only.
- `Source/` — session‑DB raw scan (`ModScanner`), manifest model + Newtonsoft game‑strict parser (`ModManifest`), superseded on the UI path by `ModEntry`.
- `Versioning/` — game `GameVersion` type + reader (`GameVersionReader`).
- `session-ses_00ea.md` at root is a stale agent‑session transcript, not source. Ignore.

## Steam launch wiring (critical)

Steam launch options run `"<launcher>" %command%`, so:
- `args[0]` = game exe path, rest = game args (`LaunchOptions.GameExecutablePath` / `GetGameArguments()`).
- `GameLocator.DiscoverGameExecutable()` falls back to registry `SteamPath` + `libraryfolders.vdf` + `appmanifest_1062090.acf` when no arg. This fallback is **only** used when the launcher runs directly (double‑click, no args); via Steam, `args[0]` already **is** the exe — do not treat Steam‑launch discovery as the primary path.
- The Steam‑locator plumbing (`SteamPath` + `libraryfolders.vdf`) is **also** required for workshop mod scanning (`GetWorkshopContentRoots`), which no arg provides — keep it.
- `-appid` and `steam://run/1062090` were tried and **REVERTED** — they make Steam re‑run the game's launch options, relaunching the launcher. Do not reintroduce.
- Chosen approach: `GameLocator.WriteSteamAppIdForGame` writes `steam_appid.txt` (content `1062090`) next to the exe, left in place, no `-appid`; if the game dir is write‑protected it falls back to the launcher folder and `MainForm` shows a copy‑file dialog.

## Direct‑launch (no mod UI)

`LaunchOptions.ShouldLaunchGameDirectly()` is true when the launcher is invoked with forwarded `-skipModManager`, or **both** `-settlementName` and `-saveName`. `MainForm_Load` then skips the UI entirely and launches straight. Keep this: the launcher is re‑invoked by Steam for save/load links.

## Behavior mirrors the game — do not simplify

- `ModPlayerPrefs` writes the **same** Unity PlayerPrefs registry keys the game reads (`HKCU\Software\Mechanistry\Timberborn`), including the djb2‑xor `_h<hash>` suffix. New persisted settings must replicate `Hash()`.
- `ModScanner` mirrors game selection: `version-*` subfolders choose the highest folder the installed game version is equal‑or‑higher than; mods with no match are omitted. Duplicate Ids get `OriginName/` prefix on `DisplayName` and feed the sort key.
- `ModSorter` replicates game load order: DisplayName sort → dependency topo‑sort → PlayerPrefs priority shift. **Important:** The dependency topo‑sort uses **both** `RequiredMods` and `OptionalMods` (verified in decompiled v1.0.13.1, Timberborn.Modding.cs ~line 757) — optional dependencies **are** considered for ordering, not ignored. `Versioning/GameVersion.IsEqualOrHigherThan` is a deliberate replica of the game's compare.
- **`ModSorter` is the single home of load order — do not split it across files.** `ModSorter.Apply(Form)` validates warnings, calls `ModSorter.ComputeLoadOrder()`, then pushes enabled + priorities to the registry. `ComputeLoadOrder()` is self‑contained against the DB tables only: reads `mods` (selected = 1) + `mod_dependencies` + `user_dependencies`, **merges `user_dependencies` into `mod_dependencies` before sorting** (both tables mean the same: `mod_id` depends on `dependency_id`, dependency loads first), orders by DisplayName then topo‑sort, and writes `priority_value` for every selected mod. Base (`2000000000`) and step (`100000`) are local vars at the top of the method.
- **Registry writes happen ONLY on "Run Game" / "Save"** — UI toggles write `EnabledValue` straight to the session DB (`AppDatabase.SetModEnabledByPath`) and never touch the registry. `ModSorter.Apply` computes the order, clears prior priorities (`ResetModPriorities`), then writes `ModEnabled` + `ModPriority` (from the DB `priority_value`, `2000000000 - 100000*rank`, rank 0 = earliest) for **every** mod — disabled ones get enabled=0 but keep their priority. Failures abort the launch. Direct‑launch re‑invocation bypasses Apply entirely.

### Critical fix to `ModSorter` (applied March 2026)

The original `SortByDependencies` conditionally removed the current mod's ID from other dependency lists **only when no duplicate ID existed**. This caused the topo‑sort to break if duplicate enabled mod IDs were present (even though they are blocked at launch).  
**Fix:** the removal is now unconditional — `currentMod.ModId` is always removed from all remaining dependency lists, restoring correct topological ordering even with duplicates. This aligns with a standard Kahn's algorithm. (See `ModSorter.cs` line 119.)

### Cycle detection for user load order

To prevent ambiguous/cyclic user‑defined load‑order rules, `AppDatabase.GetUserDependencyCycles()` detects cycles in the user‑dependency graph. Rows mean the same as `mod_dependencies` rows (`mod_id` depends on `dependency_id`), so the edge is `mod_id` → `dependency_id`; `'conflicts'` rows are not ordering edges and are excluded. Each cycle found is added as a **blocking warning** in `ModValidator.RefreshWarnings()`. When the user tries to Run Game, if any cycle exists, the Warnings view opens and launch is aborted. The user must adjust their rules to break the cycle.

## Version source

Game version read from `<exe dir>\Timberborn_Data\StreamingAssets\Version.txt`.

## Game source (decompiled reference)

`C:\Users\calloatti\source\repos\timberborn-decompiled-1.0.13.1-b769e88-sw` — decompiled Timberborn v1.0.13.1. Read it to verify launcher logic mirrors the game exactly (PlayerPrefs keys, sorter, version compare).

## Storage (SQLite via Microsoft.Data.Sqlite)

For now (debugging): **one** on‑disk SQLite DB for everything — file name = exe name + `.db` (i.e. `TimberbornLauncher.db`, next to the exe). Persistent tables: `profiles`, `profile_mods`, `user_dependencies`, `app_state` (KV store). Session/temp tables (`mods`, `mod_dependencies`) live in the same DB but are **TRUNCATED at every launch** — they're regenerated from disk. (Earlier split — file‑backed profiles DB + RAM‑only `Data Source=:memory:` session DB — deferred; do not point persistent queries at temp tables or vice versa while they share one file.) Table names are plural.

- **Profiles DB** — same single on‑disk file (persists across launches). Stores mod profiles: `id`, `date created`, `name`, `description`, `game version` (at save time). Content = ordered list of mod entries, each with `mod Id` + `source` (`local` / `steam`, disambiguates same‑Id duplicates) + `mod version`. Full CRUD: create, apply, overwrite, delete, rename, edit description. Apply maps stored (source, Id) → currently‑installed mods, enables + orders via existing PlayerPrefs registry, skips missing. Saved‑version vs current‑version mismatch warns on apply.
- **`user_dependencies` table** — persistent user load‑order rules (survives restarts, replaces priority‑up/down UI). Columns: `hash` (PK), `mod_id`, `dependency_type`, `dependency_id`. **Rows mean the exact same thing as `mod_dependencies` rows:** `mod_id` depends on `dependency_id`, so `dependency_id` loads first. `dependency_type` is `'optional'` for load‑order rules, `'conflicts'` for conflict rules (conflicts are not ordering edges and are excluded from sorting/cycle detection). `InsertUserDependency` blocks self‑dependency and rejects inserts whose inverted hash (dep, type, mod) already exists (prevents circular/redundant pairs). `ComputeUserDependencyKey` hashes `modId|type|depId` (lowercased) with `hash = hash*33 ^ c` → 8‑hex string. Table stores mod Ids only (no source). Displayed in the user load‑order view; consumed by `ModSorter.ComputeLoadOrder()` (merged with `mod_dependencies`).
- **ModPriority writing** — priorities are computed for **all** mods (disabled included) at `ModSorter.ComputeLoadOrder()`: starts at `2000000000` with step `-100000` (rank r → `ModPriority = 2000000000 - 100000*r`, rank 0 = earliest), written to the session DB (`mods.priority_value`), then pushed to the registry on Run Game/Save. Verified vs decompiled v1.0.13.1: the game sorts ascending by `originalIndex - ModPriority` — higher priority loads **EARLIER**, so descending values put rank 0 first as intended. The game then rewrites every nonzero priority to `originalIndex - finalIndex` (Timberborn.Modding.cs ~line 744), so stored values never survive a game run — by design we re‑write them next launch. Base 2000000000 keeps our slots far from default `0`; the 100000 magnitude exceeds any `originalIndex` jitter while there are < 100000 mods, so dependency‑sort reindexing can't flip adjacent ranks. `ComputeLoadOrder()` **self‑gates** via a DB‑backed dirty flag: SQLite triggers on `user_dependencies` (AFTER INSERT/UPDATE/DELETE) write `user_dependencies_last_modified = strftime('%Y-%m-%d %H:%M:%f','now')` into the `app_state` KV table; `ComputeLoadOrder` compares that against its in‑memory copy (sentinel `__never_computed__` at startup) and early‑returns when equal. So it runs once per process and after every user‑dependency edit, and **no call site checks anything** — `MainForm.ShowLoadOrderView` and `Apply` both call it unconditionally. `PanelGameLoadOrder` itself only reads `mods.priority_value` (sorted `DESC`).
- **Session tables** — temp/transient (`mods`, `mod_dependencies`), same on‑disk DB while debugging. **TRUNCATED** at every launch, rebuilt from disk each start; do not point persistent queries at them.

Source values in our storage are **`local` / `steam`** ONLY (never "Local"/"Steam Workshop"). But the game's PlayerPrefs registry keys use `Local` / `Steam Workshop` — translate when generating key names.

**UI is exclusively DB‑driven.** The mod grids (`PanelHumanOrder`, `PanelGameLoadOrder`, `PanelUserLoadOrder` grids 1 & 2) and `PanelWarnings` bind to the **shared** `AppDatabase.GetModsGridTable(string orderBy)` — the single home of the `SELECT ... FROM mods WHERE selected = 1` grid query (`orderBy` interpolated verbatim, callers pass fixed literals: `"name"` or `"priority_value DESC"`; glyph consts `AppDatabase.CharEnabled`/`CharDisabled` = `☒`/`☐` live here too). `MainForm.UpdateSummary` and `ModSorter.Apply` consume `AppDatabase.GetModList()` → `ModEntry` (selected rows only). `ModSorter.ComputeLoadOrder()` reads the session tables directly (not via `GetModList`). The registry is read **once** into the session tables at scan time and never queried by the UI; the **only** out‑of‑DB step is `ModSorter.Apply` pushing DB state (enabled + priorities for all mods) into PlayerPrefs on Run Game/Save.

### Manifest parsing (Newtonsoft, game‑strict)

`ModManifest.TryReadFile` parses manifests the same way the game does: Newtonsoft `JObject.Parse` (lenient on trailing commas and raw newlines — the strict `System.Text.Json` previously dropped mods the game loads), plus the game's `":-.," → ":0.0,"` parity tweak. It rejects what the game rejects — missing/non‑string `Id`/`Name`/`Version`/`MinimumGameVersion`, invalid version strings (game's `Version.Create` rules: empty rejects, `v` legacy OK, else dot‑separated ints), non‑array dependency lists, dependency entries without an `Id` — and **never throws**: every failure is logged to `TimberbornLauncher.log` and yields null. `ModScanner.TryReadManifest` delegates to it. `ModRepository.cs`/`ModInfo.cs`/`ModGridColumns.cs` and `AppDatabase.LoadModStates` were deleted as dead code (March 2026).

### Session DB — mod scan schema

Scan runs at launch in 3 stages, all inside one transaction: 1) raw dump of every manifest into `mods`, 2) game‑mirror selection marks `selected` per mod root, 3) registry keys/values read for all rows (version‑agnostic, cached per source/origin/modId) + dependency rows for the selected manifest only. Full schema created up front, no later ALTERs.

**`mods`** — one row per manifest.json found (mod root **and** each `version-*` subfolder):

- `mod_path` (PK) — manifest.json path
- `source` (`local` / `steam`), `origin_name` (local folder name / workshop item id)
- `version_folder` (`version-*` name or `root`)
- `mod_id`, `name`, `version`, `description`, `minimum_game_version` — manifest fields
- `selected` (0/1) — row made the game‑mirror list (best version-* for installed game version, omit no‑match)
- `enabled_registry_key`, `priority_registry_key` — full PlayerPrefs key names (`ModEnabled`/`ModPriority.{DisplaySource}.{originName}.{modId}` + `_h`+djb2 hash). Registry keys/values are version‑agnostic, so every manifest row of a mod carries the same values (read once per unique source/origin/modId).
- `enabled_value`, `priority_value` — registry values for those keys

**`mod_dependencies`** — child, real FK `mod_path` → `mods.mod_path` (unique target; same mod in both sources → distinct rows via path, independent deps):

- `mod_path` (FK), `mod_id` (owning mod's id), `dependency_type` (required/optional), `dependency_id`, `minimum_version`

Dry rows (raw dump) store empty registry keys + default values; only the selected manifest's dependencies are written. Dependency reads (`LoadDependencies`) additionally JOIN on `mods.selected = 1` — non‑selected rows are just the same mod's variants for other game versions, their dependencies would pollute the active list.

Scanned folders mirror the game: local `Documents\Timberborn\Mods\*`, workshop `steamapps\workshop\content\1062090\*` per Steam library.

## Mod list UI (implemented)

Main window: plain `Panel _viewContainer` on the left (views are `UserControl`s docked `Fill`, shown/hidden/brought‑to‑front — **no MDI**, not `IsMdiContainer`), button column on the right.

- **Human order** (default view): alphabetically sorted mod list; live `SearchTextBox` filters it; checkboxes filter by enabled/disabled and local/steam (initially unchecked = no filter); sortable by clicking column headers. Enabled toggles (glyph cell ☒/☐) write the session DB (`AppDatabase.SetModEnabledByPath`) — no registry write until "Run Game"/"Save changes".
- **Game load order**: button click calls `ModSorter.ComputeLoadOrder()` first (`MainForm.ShowLoadOrderView`), then shows mods sorted by `mods.priority_value DESC` straight from the session DB — always the current DB state, not last-Apply/scanned leftovers. Same search/filters and enabled‑toggle behavior; columns not sortable.
- **User load order**: 3 stacked grids — grid 1 select Mod 1 (`SearchTextBox` filter, Enable column non‑interactive), grid 2 select Mod 2 (`SearchTextBox` filter, Enable non‑interactive), grid 3 shows existing `user_dependencies` rows (Mod / Type / Dependency). Buttons: "Load before" (inserts `(mod2, 'optional', mod1)` — mod2 depends on mod1, so mod1 loads before mod2), "Load after" (inserts `(mod1, 'optional', mod2)` — mod1 depends on mod2, so mod2 loads before mod1), "Delete" (removes selected grid‑3 row by hash).
- **Warnings**: DB‑driven grid (`ModValidator` runs pure‑SQL checks against the session DB — duplicate enabled mod ids across sources, missing required dependencies, and cycles in user load order), `SearchTextBox` filters `Message`. All warnings are blocking. Run Game and Save changes both check `GetBlockingWarningCount()`; if > 0, the Warnings view opens and Apply aborts (no registry write, no launch). The Warnings view control is created **once** (like the other views — `MainForm.ShowWarningsView` no longer disposes/recreates it); its data refreshes on every open via `VisibleChanged` → `LoadWarnings` (`ModValidator.RefreshWarnings` + reload from DB).
- Right‑side button column: "Human order", "Game load order", "User load order", "Warnings" are `RadioButton`s (`Appearance = Button`, `TextAlign = MiddleCenter`) sharing one Form container = one toggle group (checked state shows the active view), then "Save changes" and "Run Game" bottom‑aligned. No "Reset load order" button. Each `ShowXView` ends with the matching radio's `.Checked = true` — **keep those lines**, they drive programmatic switches that fire no Click (`MainForm_Load` → Human order, blocking warnings → Warnings view).
- Views refresh from the session DB on every `VisibleChanged` (all four panels); column auto‑sizing runs once per grid (Layout handler unsubscribes itself, `SavedFillWeights` reused across panels).
- All four panels share the same top structure: `MainTable` (`TableLayoutPanel`, `Padding 0`, 1 col 100%) → row 0 `Absolute 36` = `TopPanel` (`Dock=Fill`, full width) → row 1 grid (`Dock=None`, `Anchor Top|Left|Right`). `TopPanel` is a single‑col `TableLayoutPanel` (the old right‑aligned `TitleLabel` was removed — the toggle radios now show the active view) with `SearchPanel` (`FlowLayoutPanel`, LeftToRight, left‑aligned search box + filter checkboxes) filling it. Search box margin `(0,4,12,4)`; checkboxes `(0,8,12,9)`; UserLoadOrder's bottom `ButtonPanel` is a right‑to‑left `FlowLayoutPanel` with the action buttons. No borders on any panel.
- **Grid height snapping** — `GridIntegralHeight.Apply(grid)` (all 6 grids): grids are `Dock=None` + `Anchor Top|Left|Right` and snap to the parent `MainTable` cell (`GetRowHeights()[GetRow(grid)]` minus margins): content fits → grid fills the cell height; overflow → largest whole‑row height ≤ cell (no partial bottom row ever). Runs from each panel's `MainTable.Resize` + grid `Resize` handlers and after every load/filter. Zero rows → no height change. All grids: `ColumnHeadersHeight = 24`, `ColumnHeadersHeightSizeMode = DisableResizing` (identical fixed headers, no user resize), `ScrollBars.Vertical` (no horizontal scrollbar, so row height × row count + header is exact).

## SearchTextBox control

`Source/SearchTextBox.cs` — custom `UserControl` containing a `TextBox` + a "Clear" button (always visible, right side). Fixed dimensions (270×28px). Properties: `Text`, `PlaceholderText`, `Font`, `ForeColor` (all forwarded to inner TextBox), `TextChanged` event (forwarded). Used in the four view panels: `PanelHumanOrder` (1), `PanelGameLoadOrder` (1), `PanelUserLoadOrder` (3), `PanelWarnings` (1).

## TODO — programmatic launch‑options editor

`config\config.vdf` holds per‑app launch options at `InstallConfigStore\Software\Valve\Steam\Apps\1062090\LaunchOptions`. Possible feature: self‑register `"<launcher>" %command%` on first run. Caveats: Steam must be closed while editing (it rewrites config.vdf on exit), atomic write + backup to avoid corrupting Steam config.
