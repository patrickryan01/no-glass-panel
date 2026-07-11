# ☢️ NUCLEAR OPTION — GLASS PANEL

**The HUD clutters my helmet. I took that personally and built a second screen.**

![status](https://img.shields.io/badge/status-v1.7.0%20·%20beta-ffb020?style=flat-square)
![license](https://img.shields.io/badge/license-MIT-39d6ff?style=flat-square)
![stack](https://img.shields.io/badge/stack-vanilla%20JS%20%2B%20C%23%2FBepInEx-79f0cf?style=flat-square)
![deps](https://img.shields.io/badge/node__modules-0-4ee08c?style=flat-square)
![fuel](https://img.shields.io/badge/fueled%20by-White%20Monster-white?style=flat-square)

An external, F-35-style **HMD glass panel** for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/). It runs in a browser on a second screen — a laptop parked next to the battlestation — so the **tactical map** and the **weapon + ammo readout** come *off* the helmet glass and onto their own panel. Fed by real telemetry straight out of the game over your LAN.

This is exactly how the actual jet does it. The F-35 helmet carries flight and targeting symbology; the tactical junk gets offloaded to the big panoramic cockpit display. My helmet should not be a billboard. So it isn't anymore.

It's a static HTML file. No build step. No `node_modules`. No 400MB of regret. You open it and it flies.

---

## 💀 Why this exists

I was flying with a visor so cluttered it looked like a LinkedIn feed. Map overlay, weapon block, ammo count — all painted across the glass I'm supposed to *look through*. That's not a HUD, that's a hostage situation.

Nuclear Option lets you toggle the map (`M`, and yes I moved it) but gives you exactly zero switches to pull the weapon/ammo block off the HMD. So I did what I always do when the tooling insults me: I built the tool. The helmet keeps the flight and targeting glass. Everything tactical goes to a panel on the desk beside me, where it belongs. Vertical integration. Or a problem. Depends who you ask.

Also I'm making videos and a real glass cockpit on a second screen looks unreasonably good on camera.

---

## 📦 Install the mod (on the gaming PC)

**One line. Standalone.** Finds your Nuclear Option install, drops in BepInEx + the plugin, opens the port. In PowerShell:

```powershell
irm https://raw.githubusercontent.com/patrickryan01/no-glass-panel/main/install.ps1 | iex
```

Rather do it by hand? Grab `GlassPanel.dll` from the [latest release](https://github.com/patrickryan01/no-glass-panel/releases/latest), make sure [BepInEx](https://docs.bepinex.dev) (5.x, Mono x64) is in the game folder, and drop the DLL into `Nuclear Option\BepInEx\plugins\`.

The **laptop needs nothing installed** — it's just a browser pointed at the gaming PC (`http://<gaming-pc-ip>:8787`).

## 🖥️ Run the panel (right now, no live game needed)

```
web/index.html  →  open it in any browser
```

That's the whole install. It boots up flying a **simulated** Nuclear Option sortie so you can see the whole thing move — a frameless F‑35-style HMD horizon (airspeed in **knots + km/h**, altitude in **feet + meters**, Mach, G, AoA), framed **Map** and **Radar** MFDs, an **RWR**, a **meatball / AoA** approach indexer, the full **stores** loadout, a **damage** silhouette, and a **comms** strip you can type into. It runs on nothing but Canvas and spite. Reads fine on a phone or tablet too.

The `SIM` pill up top flips to `LIVE` the moment the mod connects and starts feeding it real numbers.

---

## 🧠 How it works

```
  Nuclear Option (Unity/Mono)
        │
        │  BepInEx plugin reads ownship telemetry each frame
        ▼
  Glass Panel Mod  ──►  WebSocket (JSON, ~30 Hz)  ──►  Browser panel on the laptop
   (this repo)                over your LAN              (web/index.html)
```

Nuclear Option is a Mono Unity game, which means it's moddable with **BepInEx**, which means the game state is reachable from a C# plugin. That's not a theory — [NOBlackBox](https://github.com/KopterBuzz/NOBlackBox) already pulls true airspeed, Mach, AoA, AGL, gear, radar mode, and raw stick/throttle inputs out of this exact game to feed Tacview. If it can be read for a black box, it can be read for a glass panel.

The panel is dumb on purpose — it just renders whatever telemetry it's handed and animates a simulation when nobody's talking to it. One data contract, two producers (the sim and the mod). Swap the source, same panel.

---

## 📡 The telemetry contract

The panel consumes one JSON object, `applyTelemetry(obj)`. Send any subset ~30 times a second; missing keys hold their last value. Full field list, units, and ranges live in **[docs/TELEMETRY.md](docs/TELEMETRY.md)**. That document is the seam between the two halves of this project — the mod's only job is to fill it out honestly.

---

## 🗺️ Where it's at

The build history and definitions-of-done live in **[ROADMAP.md](ROADMAP.md)**. Where it stands now — **everything below is fed by verified game reads, no guessing:**

**Live:**
- Full multi-MFD panel — Map + Radar, frameless HMD horizon, RWR, meatball, stores, data band. Mobile/tablet responsive.
- Real flight telemetry — airspeed (kt + km/h), altitude (ft + m), Mach, G, AoA, V/S, heading, radar alt, fuel, engine thrust, full loadout, live HOTAS inputs.
- **RWR** (incoming missiles) and a **damage** silhouette that only shows when you're hit.
- **Bi-directional comms** — type into game chat from the panel; game chat comes back to it.
- **Touchscreen control** — cycle weapons, drop gear, pop flares from a tablet.
- **HMD declutter** — optionally hide the weapon/ammo block on the in-game visor (the original point).
- **OBS overlay** (`?overlay`) and **per-device views** (`?view=hud|map|radar|rwr`).

- **Datalink** — a Link-16-style shared track picture off the faction HQ (dashed tracks, distinct from your own sensor).
- **Fuller RWR** — radars *painting* you, not just missile launches.
- **Nav / RTB** — bearing + range to your nearest airbase, drawn as a steer line on the map.

**On the board:**
- Mission objectives (blocked on a game type that won't decompile — no guessing).
- Thunderstore packaging.

---

## 🧰 Stack

- **Panel:** HTML + Canvas + vanilla JS. Zero dependencies. Zero build. Deploys like a fist through drywall.
- **Mod:** C# on BepInEx (Mono). Reads the game, serves a WebSocket, minds its own business.

## 🧪 Beta — kick the tires, file the bugs

This is **beta**. It flies, it's live, and it absolutely still has sharp edges. If a readout looks wrong, a module renders weird, or you want a gauge that isn't there — I want to hear it. Support lives right here in the repo, not in some Discord-only cave.

- 🐛 **Bugs** → open an [issue](https://github.com/patrickryan01/no-glass-panel/issues/new/choose). Tell me the aircraft, what the **panel showed vs. what the game showed**, and paste `BepInEx\LogOutput.log` if it didn't load.
- 💬 **Ideas, questions, show off your battlestation** → [Discussions](https://github.com/patrickryan01/no-glass-panel/discussions).
- 🔧 **Pull requests welcome** → see [CONTRIBUTING.md](CONTRIBUTING.md). Two-space indent. I will die on that hill. Alone. With my White Monster.

---

## ⚖️ License

MIT. See [LICENSE](LICENSE). Do what you want with it. It's a HUD panel for a video game, not the nuclear football. (The in-game one, however, is entirely your problem.)

---

*Built in Cleveland, between a White Monster and a sortie that turned out fine. Say sorry to your mom.*
