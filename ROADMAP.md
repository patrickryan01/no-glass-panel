# 🗺️ ROADMAP

The plan, phased. Every phase has a **Definition of Done** so "done" means done, not "works on my machine" (it made everyone's machine's problem last time).

Guiding rule for the whole thing: **no guessing at game internals.** Anything that reads Nuclear Option's state gets verified against the actual compiled assembly and cross-checked with a known-good reference ([NOBlackBox](https://github.com/KopterBuzz/NOBlackBox)) before it ships. Real code or no code.

---

## Phase 0 — Recon ✅

Confirm the project is even possible before writing a line of it.

- [x] Nuclear Option is Mono/Unity → moddable via **BepInEx** (confirmed: `Assembly-CSharp.dll` is Mono, not IL2CPP).
- [x] Telemetry is reachable from a plugin — proven by NOBlackBox already reading TAS/Mach/AoA/AGL/gear/inputs from this game.
- [x] Reference DLLs present locally for building against: `Assembly-CSharp.dll`, `Rewired_Core.dll`, `UnityEngine.CoreModule.dll`.

**DoD:** we know it can be done and what it takes. ✅

---

## Phase 1 — The Panel ✅

A self-contained glass cockpit that runs today on a simulation, so the design and the data contract are locked before any mod exists.

- [x] F-35 HMD aesthetic, dark single-theme, phosphor-green symbology.
- [x] Primary flight display: horizon + pitch ladder, bank pointer, boresight, velocity vector, airspeed/altitude tapes, heading tape.
- [x] Stores panel (selected weapon + ammo — the thing coming off the HMD), countermeasures, fuel + endurance, heading-up tactical radar, live HOTAS input bars, master caution/warning.
- [x] Driven by one `applyTelemetry()` contract; simulates a sortie when no live feed is connected.
- [x] `SIM DATA` → `LIVE · MOD` connection state, `connectLive(url)` WebSocket seam stubbed in.

**DoD:** open `web/index.html`, watch it fly, and the JSON contract the mod must satisfy is written down. ✅

---

## Phase 2 — Real Symbols 🔧 (current)

Stand up the toolchain and extract the **actual** telemetry field names from the game. This is the "no guessing" phase.

- [x] Install .NET SDK 8 + decompiler (`ilspycmd`) — build & inspection toolchain up.
- [ ] Install BepInEx into the Nuclear Option install (test rig).
- [x] Decompile `Assembly-CSharp.dll` and identify the real ownship/aircraft classes and fields: airspeed, altitude, AGL, heading, pitch/roll, AoA, fuel, throttle, gear, selected weapon + ammo, countermeasures, targets.
- [x] Cross-verify every field against NOBlackBox's source so we're standing on a proven read, not a hopeful one.
- [x] Write it all down in `docs/GAME_SYMBOLS.md` — the verified map from game member → contract field.

**DoD:** a documented, verified list of exactly which game members produce each telemetry value. Nothing in it is a guess.

---

## Phase 3 — The Mod

Turn the verified symbols into a plugin that reads the game and serves the panel.

- [x] BepInEx plugin project (`mod/`) referencing the real game DLLs.
- [x] Read verified telemetry once per frame; assemble the `docs/TELEMETRY.md` JSON.
- [x] Host a lightweight WebSocket server (also serves the panel page); broadcast at a configurable rate.
- [x] BepInEx config: port + update Hz. (binds all interfaces; unit prefs later)
- [x] **Compiles clean against the real assemblies** — 0 warnings, 0 errors. The symbols are real by construction.

**DoD:** plugin builds and installs to `BepInEx/plugins`. Runtime proof (loads in-game, panel receives live JSON) is the first task of Phase 4 — needs the game actually running.

---

## Phase 4 — Live + Declutter

Wire the panel to the real jet and clean up the helmet.

- [ ] Panel connects to the mod over the **LAN** from the laptop; `LIVE · MOD` goes green with real numbers.
- [ ] Mod hides the **weapon-selected + ammo** element on the HMD (the map already toggles natively on `M`).
- [ ] Reconnect logic — mission reload / menu / respawn don't kill the feed.

**DoD:** fly the game, glance at the laptop, and it's your actual airspeed/stores/radar — with the helmet glass now clean.

---

## Phase 5 — Polish + Ship

Make it something other people (and a camera) can use.

- [ ] OBS browser-source layout so the panel drops straight into a stream/recording.
- [ ] Styling + readability passes at recording resolution.
- [ ] Package the mod (Thunderstore-ready), tag a release, write install docs.

**DoD:** someone who isn't me can install it, point it at their game, and see their own jet on a second screen.
