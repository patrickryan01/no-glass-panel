# ☢️ NUCLEAR OPTION — GLASS PANEL

**The HUD clutters my helmet. I took that personally and built a second screen.**

![status](https://img.shields.io/badge/status-v1.1.0%20·%20live-4ee08c?style=flat-square)
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

That's the whole install. It boots up flying a **simulated** Nuclear Option sortie — attitude, airspeed and altitude tapes, heading, AoA, G, fuel and endurance, a stores panel that cycles weapons, countermeasures depleting, a heading-up tactical radar, and live HOTAS input bars. It runs on nothing but Canvas and spite.

The `SIM DATA` pill up top flips to `LIVE · MOD` the moment the mod (Phase 2) connects and starts feeding it real numbers.

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

## 🗺️ Where this is going

Phased plan and definitions-of-done in **[ROADMAP.md](ROADMAP.md)**. Short version:

- **Phase 1 — The Panel** ✅ — self-contained HMD dashboard, simulated feed, contract locked.
- **Phase 2 — Real Symbols** 🔧 — toolchain + decompile the game and pull the *actual* telemetry field names. No guessing. Verified against the running assembly and NOBlackBox.
- **Phase 3 — The Mod** — BepInEx plugin: read verified telemetry, host the WebSocket, broadcast the contract.
- **Phase 4 — Live + Declutter** — panel goes live over LAN; mod hides the weapon/ammo block on the HMD.
- **Phase 5 — Polish + Ship** — OBS browser-source, packaging, release.

---

## 🧰 Stack

- **Panel:** HTML + Canvas + vanilla JS. Zero dependencies. Zero build. Deploys like a fist through drywall.
- **Mod:** C# on BepInEx (Mono). Reads the game, serves a WebSocket, minds its own business.

## ⚖️ License

MIT. See [LICENSE](LICENSE). Do what you want with it. It's a HUD panel for a video game, not the nuclear football. (The in-game one, however, is entirely your problem.)

---

*Built in Cleveland, between a White Monster and a sortie that turned out fine. Say sorry to your mom.*
