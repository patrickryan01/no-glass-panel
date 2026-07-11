# ☢️ Glass Panel

**The HUD clutters my helmet. I took that personally and built a second screen.**

An external, F-35-style **HMD glass panel** for Nuclear Option. It runs in a browser on a laptop or tablet parked next to your battlestation, fed by real telemetry straight out of the game over your LAN — so the **tactical map** and the **weapon + ammo block** come *off* the helmet glass and onto their own panel, where they belong.

This is how the actual jet does it. The F-35 helmet carries flight and targeting symbology; the tactical junk gets offloaded to the big panoramic display. My helmet should not be a billboard. So it isn't anymore.

## Install

1. Install with the mod manager (it pulls **BepInExPack** for you).
2. Launch the game once so the plugin opens its port.
3. On your laptop or tablet — same network — open a browser to **`http://<your-gaming-pc-ip>:8787`**.

That's it. The laptop needs nothing installed; it's just a browser. The panel boots flying a **simulated** sortie and flips to **LIVE** the moment the game feeds it.

## What's on the panel

- Frameless F-35-style **HMD horizon** — airspeed (kt + km/h), altitude (ft + m), Mach, G, AoA, V/S, heading.
- Framed **Map** and **Radar** MFDs, an **RWR**, a **meatball / AoA** approach indexer, the full **stores** loadout, and a fuel/endurance readout.
- **Aircraft-aware damage** silhouette — labeled with your airframe, lights up the section that actually got shot off.
- **Datalink** (Link-16-style shared tracks), **Nav / RTB** steering to the nearest airbase, and the live **mission objectives** checklist over the map.
- **Bi-directional comms** — type into game chat from the panel; game chat comes back.
- **Touchscreen control** — cycle weapons, drop gear, pop flares from a tablet.
- **OBS overlay** (`?overlay`) and **per-device views** (`?view=hud|map|radar|rwr`).

## Beta

It flies, it's live, and it still has sharp edges. Bugs, ideas, and battlestation photos all go to the repo — **https://github.com/patrickryan01/no-glass-panel**. MIT licensed. Two-space indent. I will die on that hill.

*Built in Cleveland, between a White Monster and a sortie that turned out fine.*
