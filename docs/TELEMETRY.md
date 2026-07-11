# 📡 Telemetry Contract

The panel and the mod meet here and nowhere else. The mod's entire job is to fill this object out honestly, ~30 times a second, and ship it over the WebSocket. The panel renders whatever it's handed.

## Transport

- **Protocol:** WebSocket, text frames.
- **Payload:** one JSON object per frame (below).
- **Rate:** ~30 Hz target. Faster is fine; the panel renders on `requestAnimationFrame` regardless.
- **Partial updates:** send any subset of keys. Missing keys keep their last value. The panel never assumes a full frame.
- **Entry point:** the browser calls `applyTelemetry(obj)` with the parsed JSON. Connecting is `connectLive('ws://<gaming-pc-ip>:<port>')`.

## Fields

| Key | Type | Unit | Notes |
|---|---|---|---|
| `ias` | number | knots | Indicated airspeed. Drives the left tape. |
| `tas` | number | knots | True airspeed. |
| `mach` | number | — | Mach number, status bar. |
| `alt` | number | feet MSL | Drives the right tape. |
| `agl` | number | feet | Height above ground; feeds the low-altitude warning. |
| `vs` | number | ft/min | Vertical speed. Signed. |
| `hdg` | number | degrees 0–360 | Magnetic heading. Drives the heading tape and radar orientation. |
| `pitch` | number | degrees | + nose up. Drives the pitch ladder. |
| `roll` | number | degrees | + right bank. Drives horizon + bank pointer. |
| `aoa` | number | degrees | Angle of attack. >15 caution, >18 warning. |
| `g` | number | g | Load factor. |
| `throttle` | number | 0–1 | Commanded throttle. |
| `gear` | boolean | — | `true` = down. |
| `fuelKg` | number | kg | Fuel remaining. Drives the fuel bar. |
| `fuelMax` | number | kg | Tank capacity, for the bar's full scale. |
| `fuelSec` | number | seconds | Endurance to bingo. <600 caution, <300 critical. |
| `weaponIndex` | number | index | Which entry in `weapons[]` is selected. |
| `weapons` | array | — | `[{ name:string, ammo:number, unit:string }]`. The stores panel. |
| `flares` | number | count | Remaining flares. |
| `chaff` | number | count | Remaining chaff. |
| `flaresMax` | number | count | Full scale for the flare bar. |
| `chaffMax` | number | count | Full scale for the chaff bar. |
| `target` | object | — | `{ name:string, rng:number(m), brg:number(deg), locked:boolean }`. |
| `contacts` | array | — | `[{ brg:number(deg), rng:number(m), type:'H'|'F'|'U' }]`. Radar blips: Hostile / Friendly / Unknown. |
| `inputs` | object | — | `{ pitch:-1..1, roll:-1..1, yaw:-1..1, throttle:0..1 }`. Raw HOTAS, for the input bars. |

## Example frame

```json
{
  "ias": 342, "tas": 361, "mach": 0.52,
  "alt": 8450, "agl": 8270, "vs": -420,
  "hdg": 104, "pitch": -2.1, "roll": 14.0,
  "aoa": 5.3, "g": 1.4, "throttle": 0.88, "gear": false,
  "fuelKg": 3120, "fuelMax": 5000, "fuelSec": 1180,
  "weaponIndex": 4,
  "weapons": [
    { "name": "GAU CANNON", "ammo": 480, "unit": "RDS" },
    { "name": "AIM-SR",     "ammo": 4,   "unit": "MSL" },
    { "name": "AGM STRIKE", "ammo": 6,   "unit": "MSL" },
    { "name": "MK82 x8",    "ammo": 8,   "unit": "BMB" },
    { "name": "NUCLEAR",    "ammo": 1,   "unit": "SPL" }
  ],
  "flares": 44, "chaff": 44, "flaresMax": 60, "chaffMax": 60,
  "target":  { "name": "SAM", "rng": 9200, "brg": 104, "locked": true },
  "contacts": [
    { "brg": 104, "rng": 9200,  "type": "H" },
    { "brg": 250, "rng": 16000, "type": "F" }
  ],
  "inputs": { "pitch": -0.14, "roll": 0.40, "yaw": 0.02, "throttle": 0.88 }
}
```

## v0.2 additions (multi-MFD panel)

The Tomcat-style panel adds these. All optional — a module shows a **standby** state until its field arrives, so the mod can light them up incrementally.

| Key | Type | Notes |
|---|---|---|
| `contacts[].alt` | number (m) | Contact altitude — shown on the radar block. |
| `contacts[].spd` | number (kn) | Contact speed. |
| `contacts[].name` | string | Contact type/callsign. |
| `contacts[].locked` | boolean | Drives the locked-target data block. |
| `rwr` | array | `[{ brg:deg, band:string, lock:0|1|2 }]` — radar warning receiver. `lock`: 0 search, 1 track, 2 launch. Empty array = no threats; **absent = RWR standby**. *(v1.2.0: fed from the game's `MissileWarning` system — incoming missiles as `band:"M"`, `lock:2`. Radar-track emitters are a later add.)* |
| `damage` | object | `{ hull:0..1, sections:{ nose, lwing, rwing, tail, engine : 0..1 } }` (1 = healthy). The damage silhouette only appears when something is < 1. |
| `chat` | array | `[{ who:string, msg:string }]` — recent in-game messages (v1.3.0: captured live from `ChatManager`). Absent = comms standby. |

**Panel → game (v1.3.0, bi-directional):** the panel can also *send*. Over the same WebSocket it posts `{"t":"chat","all":true,"text":"..."}`; the mod drains it on the main thread and calls `ChatManager.SendChatMessage(text, allChat)`. That's how you type into game chat from the laptop/tablet.

**Panel → game commands (v1.4.0, touchscreen):** the panel also posts `{"t":"cmd","a":"gear|wpn+|wpn-|cm"}` — the mod maps these on the local aircraft to `SetGear(!gearDeployed)`, `weaponManager.NextWeaponStation()` / `PreviousWeaponStation()`, and `countermeasureManager.PopFlares()`. That's the touchscreen MFD control.

**v1.4.0 telemetry additions:** `g` (real, `aircraft.gForce`), `ias` (now air-density corrected from TAS), `engine` (total thrust, kN), and `weapons` now carries the **full loadout** (every station), with `weaponIndex` marking the selected one.

**v1.5.0:** `datalink` — `[{ brg:deg, rng:m, type:'H'|'F' }]`, the faction HQ's shared track picture (`FactionHQ.trackingDatabase`), drawn dashed/dimmer on the map to distinguish it from own-sensor `contacts`. And `rwr` now also carries radars *painting* you (`band:"R"`, `lock:1`, from `Aircraft.onRadarWarning`) alongside missile launches (`band:"M"`, `lock:2`).

The `meatball` / AoA indexer needs no field — it's derived on the panel from `vs` + `tas` (flight-path angle vs a ~3.5° glideslope) and `aoa` (on-speed indexer), so it works for any airframe.

## Units note

Speeds and altitudes here are aviation-standard (knots / feet) because that's how the panel is labeled. Nuclear Option works internally in metric (m/s, meters). Converting metric → these units is the **mod's** responsibility (Phase 3), so the panel stays a dumb, honest renderer. The exact source fields get pinned down and documented in `GAME_SYMBOLS.md` during Phase 2 — verified against the game, not guessed.
