# 🔧 mod/ — Glass Panel telemetry mod (BepInEx)

The C# BepInEx plugin that reads Nuclear Option and feeds the [panel](../web/index.html). It reads the local aircraft every frame, serializes the [telemetry contract](../docs/TELEMETRY.md), and serves both the panel page **and** the live WebSocket feed on one port. No external libraries — the HTTP+WebSocket server is hand-rolled on raw sockets. On brand.

Every game read is a **verified** member — see [../docs/GAME_SYMBOLS.md](../docs/GAME_SYMBOLS.md). It compiles against the real `Assembly-CSharp.dll`, so "it builds" means "the symbols are real."

## Layout

| File | Job |
|---|---|
| `GlassPanel/Plugin.cs` | BepInEx entry — config (port, Hz), starts the server, pushes a frame each `Update()`. |
| `GlassPanel/TelemetryReader.cs` | Reads `Aircraft` → builds the JSON. The verified reads live here. |
| `GlassPanel/MiniServer.cs` | Dependency-free HTTP + WebSocket server on one port. |
| `GlassPanel/GlassPanel.csproj` | net472 class library; references BepInEx + game DLLs; embeds the panel page. |

## Build + install

Prereqs: **.NET SDK 8**, and **BepInEx** installed in the game folder.

```powershell
# builds and drops GlassPanel.dll into <game>\BepInEx\plugins
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Nuclear Option"
```

Or by hand:

```powershell
dotnet build .\GlassPanel\GlassPanel.csproj -c Release -p:NuclearOptionDir="<game path>"
copy .\GlassPanel\bin\Release\net472\GlassPanel.dll "<game path>\BepInEx\plugins\"
```

## Use it

1. Launch Nuclear Option (BepInEx loads the plugin; check `BepInEx\LogOutput.log` for `Glass Panel up`).
2. On the laptop, open **`http://<gaming-pc-ip>:8787`**. The page serves itself from the mod and auto-connects to the live feed on the same port.
3. Jump in a jet. The panel flips from `SIM DATA` to `LIVE · MOD` and starts showing your actual numbers.

Config (port, update Hz) lands in `BepInEx\config\ai.fireballz.noglasspanel.cfg` after first run.

## Known Phase-4 refinements (flagged, honest)

- Full weapon **loadout list** — currently sends the *selected* station (which is the point: weapon + ammo off the HMD). Enumerating all stations is a follow-up.
- **Flare vs chaff** split is inferred from each countermeasure station's `threatTypes`; confirm the classification in the air.
- **G-force** and true **IAS** (air-density corrected) are the two contract fields not yet sourced — TAS stands in for IAS, and G is unset pending a clean read.
