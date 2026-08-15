# Timberborn Launcher

WinForms GUI launcher (net8.0-windows) that manages Timberborn mods and launches the game directly, skipping the game's built-in mod manager.

## Features

- **Mod list** — every installed local (`Documents\Timberborn\Mods`) and Steam Workshop mod, with enabled/disabled state read straight from the game's PlayerPrefs registry.
- **Game load order** — mirrors the game's exact sorting (display name → dependency topo-sort, optional deps included → priority shift) and pushes the order to the registry on Run/Save.
- **User load order** — persistent custom "mod X loads before/after mod Y" rules, merged into the dependency graph; cycles are detected and block launch.
- **Conflict rules** — "mod1 conflicts mod2" without ordering semantics.
- **Profiles** — save, apply, overwrite, rename, delete named mod loadout profiles.
- **Warnings view** — blocks launch on duplicate enabled mod IDs, missing required dependencies, and user-order cycles.
- **Direct-launch mode** — when invoked by Steam with `-skipModManager` / save links, bypasses the UI and launches straight.

## Requirements

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Timberborn via Steam

## Setup

The launcher must be the game's launch target. In Steam:

1. Right-click **Timberborn** → **Properties** → **Launch Options**.
2. Set them to: `"<path-to>\TimberbornLauncher.exe" %command%`

Steam then passes the game executable path and args to the launcher, which starts the game after applying the mod state. When run by double-click instead (no args), it locates the game via the Steam library folders on disk.

## Build

```
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `bin\Release\net8.0-windows\win-x64\publish\`.

The app writes a single SQLite DB (`TimberbornLauncher.db`) next to the executable and a log file (`TimberbornLauncher.log`).
