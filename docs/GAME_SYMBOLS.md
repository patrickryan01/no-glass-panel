# 🔬 Verified Game Symbols

The map from Nuclear Option's real innards to the [telemetry contract](TELEMETRY.md). **Nothing here is a guess.** Two sources, both checkable:

- **NOBlackBox** — [KopterBuzz/NOBlackBox](https://github.com/KopterBuzz/NOBlackBox), a production Tacview exporter that reads this exact game. Anything tagged `[NBB]` is a read it already does in the field. Proven.
- **Decompile** — `ilspycmd` on the game's own `Assembly-CSharp.dll` (with the game's `Managed/` folder as reference path). Anything tagged `[DEC]` I confirmed against the actual compiled class members.

If a value below can't be traced to one of those tags, it doesn't ship until it can.

## Getting the ownship

```csharp
Aircraft ownship;
GameManager.GetLocalAircraft(out ownship);   // [NBB] the player's own aircraft
```

`Aircraft : Unit, IRadarReturn, IRearmable, IRefuelable`. Everything below hangs off that `Aircraft` (or its `Unit` base).

## The map

| Contract field | Game member | Src | Unit / conversion |
|---|---|---|---|
| `tas` | `aircraft.speed` | `[NBB]` | m/s → kn ×1.94384 |
| `ias` | `aircraft.speed` (approx; true IAS needs air density) | `[NBB]` | m/s → kn; refine in Phase 3 |
| `mach` | `aircraft.speed / 340f` | `[NBB]` | NOBlackBox's own formula |
| `alt` | `unit.transform.position.GlobalY()` | `[NBB]` | m MSL → ft ×3.28084 (floating-origin extension) |
| `agl` | `aircraft.radarAlt` | `[NBB]` | m → ft; clamp ≥ 0 |
| `vs` | `aircraft.cockpit.rb.velocity.y` | `[NBB]` | m/s → ft/min ×196.85 |
| `hdg` | `unit.transform.eulerAngles.y` | `[NBB]` | degrees |
| `pitch` | `unit.transform.eulerAngles.x` (x>180 ? 360−x : −x) | `[NBB]` | degrees, adjusted per NBB |
| `roll` | `unit.transform.eulerAngles.z` (z>180 ? 360−z : −z) | `[NBB]` | degrees, adjusted per NBB |
| `aoa` | `atan2(v.y, v.z) × −57.29578`, where `v = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity)` | `[NBB]` | degrees |
| `throttle` | `aircraft.GetInputs().throttle` | `[NBB]` | `ControlInputs` struct, 0..1 |
| `inputs.{pitch,roll,yaw,throttle}` | `aircraft.GetInputs().{pitch,roll,yaw,throttle}` | `[NBB]` | −1..1 (throttle 0..1) |
| `gear` | `aircraft.gearDeployed` | `[NBB]` | bool |
| `fuelKg` | `aircraft.GetFuelQuantity()` | `[DEC]` | absolute quantity; `fuelLevel` (0..1) also present |
| `fuelMax` | `aircraft.RecalcFuelCapacity()` / sum of `GetFuelTanks()` | `[DEC]` | capacity; confirm units in Phase 3 |
| `weapons[].` / `weaponIndex` | `aircraft.weaponManager` → stations; `currentWeaponStation` is selected | `[DEC]` | see below |
| `weapons[].name` | `currentWeaponStation.WeaponInfo` (display-name field TBC) | `[DEC]` | `WeaponInfo` is the weapon def |
| `weapons[].ammo` | `currentWeaponStation.Ammo` | `[DEC]` | int; `GetAmmoReadout()` returns a ready string |
| (ammo max) | `currentWeaponStation.FullAmmo` | `[DEC]` | int |
| `flares` / `chaff` | `aircraft.countermeasureManager` → station `ammo` / `maxAmmo`; `GetFlareAmmoProportion()` | `[DEC]` | flare vs chaff station split TBC in Phase 3 |
| `target` / `contacts` | `aircraft.weaponManager.GetTargetList()` → `List<Unit>` | `[NBB]` | first = locked; each `Unit` has `.persistentID`, `.transform` |
| (radar on) | `aircraft.radar.activated` | `[NBB]` | bool |
| `g` | not a direct member — derive from `rb.velocity` change, or locate accelerometer | — | Phase 3: compute, then verify |

## Still to pin down in Phase 3 (flagged, not guessed)

These have a verified *entry point* but one more member to confirm while writing the plugin against the loaded assembly:

- **Weapon display name** — `WeaponStation.WeaponInfo` is confirmed; its human-readable name field (`.name` / `.weaponName` / display string) gets confirmed live. `GetAmmoReadout()` is a good fallback for the ammo string.
- **Full weapons list** — enumerate `weaponManager`'s stations (via `GetCurrentLoadout()` or the station list) to populate `weapons[]`; `currentWeaponStation` gives the selected index.
- **Flare vs chaff** — `CountermeasureManager` exposes per-station `ammo`/`maxAmmo` and `GetFlareAmmoProportion()`; confirm which station/index is flare vs chaff.
- **G-force** — no clean direct field spotted; compute from the cockpit rigidbody, then sanity-check against the in-game readout.

## Update cadence

NOBlackBox gates reads behind a configurable `aircraftUpdateDelta` and only emits changed values. The glass panel wants smooth motion, so the plugin reads every fixed step and pushes the full frame at the configured Hz — the panel does its own interpolation on `requestAnimationFrame`.
