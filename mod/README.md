# 🔧 mod/ — Glass Panel telemetry mod (BepInEx)

This is where the C# BepInEx plugin lives. It is **not written yet, on purpose.**

The whole project runs on one rule: no guessing at the game's internals. This plugin reads live state out of Nuclear Option and broadcasts the [telemetry contract](../docs/TELEMETRY.md) over a WebSocket. Reading the wrong field and shipping it would make the panel confidently lie, which is worse than an empty panel. So the game reads don't get written until they're **verified** (Phase 2 in the [roadmap](../ROADMAP.md)).

## What has to happen before code lands here

1. **Toolchain** — .NET SDK + BepInEx dev setup (not yet installed on the dev box).
2. **BepInEx in the game** — install it into the Nuclear Option folder as the test rig.
3. **Verified symbols** — decompile `Assembly-CSharp.dll`, identify the real aircraft/ownship classes and fields, cross-check every one against [NOBlackBox](https://github.com/KopterBuzz/NOBlackBox), and write the mapping into `docs/GAME_SYMBOLS.md`.

## What it will do once those are true (Phase 3)

- Reference the real game DLLs (present locally under the game's `NuclearOption_Data/Managed/`).
- Read verified telemetry once per frame.
- Assemble the JSON from `docs/TELEMETRY.md`.
- Host a small WebSocket server and broadcast to any connected panel at a configurable rate.
- Expose a BepInEx config: bind address, port, update Hz, unit preferences.

Until every game read in here is backed by a verified symbol, this folder stays a plan, not a plugin.
