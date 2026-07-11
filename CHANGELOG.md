# Changelog

Every version, what it changed, and why. Newest first. Dates are when the tag was cut.

The through-line: **no guessing at game internals.** Every telemetry read is verified against the compiled assembly and cross-checked with [NOBlackBox](https://github.com/KopterBuzz/NOBlackBox) before it ships. If a number's origin couldn't be proven, it didn't get drawn.

## v1.8.1

- **Panel falls back to the demo when the live feed drops.** When a mission ends — menu, respawn, or you fly it into a mountain — the mod stops broadcasting. The panel used to freeze on the last frame until the socket closed. Now a ~1.2s staleness timeout drops it back to the simulated sortie, so an idle panel always shows the demo and a live mission auto-switches to real data, both directions, no button.
- **The `SIM` / `LIVE` pill tells the truth.** It tracks actual telemetry flow now instead of just whether the socket is open — so it reads `SIM` while the demo plays and `LIVE` only while the game is genuinely feeding it.

## v1.8.0

- **Mission objectives.** The current mission's objective list, live, overlaid on the map: `○` in progress, `✓` complete, `✗` failed. Read from `MissionManager.Objectives.AllObjectives` — display text, state, and completion percent — verified against the real `ObjectiveV2.Objective` type. Empty when no mission is loaded.

## v1.7.0

- **Cockpit rework.** Reorganized the panel layout for readability at a glance.
- **Aircraft-aware battle-damage display.** The damage silhouette is labeled with your actual airframe and lights up the exact section — nose, wing, tail, engine — that just got shot off.

## v1.6.0

- **Nav / RTB.** Bearing and range to your nearest airbase, drawn as a steer line — for when the jet's on fire and you'd like to land the parts that are left.

## v1.5.0

- **Datalink.** A Link-16-style shared track picture off the faction HQ, drawn distinct from your own sensors.
- **Fuller RWR.** Radars painting you *and* missiles in the air, before they become your problem.

## v1.4.0

- **Completeness pass** on the real reads: true G, IAS, engine thrust, and full loadout.
- **Touchscreen control** — cycle weapons, drop gear, and pop flares from a tablet.
- **HMD declutter** — optionally strangle the weapon/ammo block on the in-game visor. The original sin, corrected.

## v1.3.0

- **Bi-directional comms.** Type into game chat from the panel; game chat comes back to it.

## v1.2.0

- **RWR and damage come off standby.** Both are now backed by real game reads instead of placeholders.

## v1.1.0

- **Refined tactical HUD** — frameless glass, dual units (kt + km/h, ft + m), and more readouts.

## v1.0.0

- **The Tomcat pit** — the real first release. The panel reworked into a proper glass cockpit.

## v0.1.0

- **You can actually install it now.** First installable build: BepInEx plugin reads verified telemetry once per frame and serves the panel over a LAN WebSocket.
