# ☢️ NUCLEAR OPTION — GLASS PANEL

**Some genius decided my canopy should double as a billboard. I disagreed, in C#.**

[![thunderstore](https://img.shields.io/thunderstore/v/RyanEngineering/GlassPanel?style=flat-square&color=39d6ff&label=thunderstore)](https://thunderstore.io/c/nuclear-option/p/RyanEngineering/GlassPanel/)
[![downloads](https://img.shields.io/thunderstore/dt/RyanEngineering/GlassPanel?style=flat-square&color=4ee08c)](https://thunderstore.io/c/nuclear-option/p/RyanEngineering/GlassPanel/)
![status](https://img.shields.io/badge/status-v1.8.1%20·%20beta-ffb020?style=flat-square)
![license](https://img.shields.io/badge/license-MIT-39d6ff?style=flat-square)
![stack](https://img.shields.io/badge/stack-vanilla%20JS%20%2B%20C%23%2FBepInEx-79f0cf?style=flat-square)
![deps](https://img.shields.io/badge/node__modules-0-4ee08c?style=flat-square)
![fuel](https://img.shields.io/badge/fueled%20by-White%20Monster-white?style=flat-square)

An external, F-35-style **HMD glass panel** for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/). It runs in a browser on whatever screen you've got parked next to the stick — laptop, tablet, the sad tablet in the kitchen — and it's fed live telemetry straight out of the game over your LAN. The **tactical map** and the **weapon/ammo block** come *off* the helmet glass and go live on their own panel, where a reasonable person keeps them.

The real jet works exactly like this. The F-35 helmet carries flight and targeting symbology and offloads the rest to a big panoramic display, because the people who build fighter cockpits understand that you cannot shoot what you cannot see. Nuclear Option, in its infinite mercy, staples the entire tactical picture directly to your eyeball and calls it a feature.

It's a static HTML file. No build step. No `node_modules`. No dependency tree that quietly grows a CVE while you sleep. You open it, it flies.

---

## 💀 Why this exists

Here is how you die in this game. Not to a missile you saw — to the one you didn't, because at the exact moment it launched, the thing you needed to notice was hiding behind a weapon readout, an ammo counter, and a map overlay, all painted across the one square foot of glass you are physically required to look through. You don't lose the fight. You lose a staring contest with your own UI, and then you lose the fight.

Nuclear Option lets you toggle the map. That's it. That's the whole mercy. There is no switch, anywhere, to peel the weapon and ammo garbage off the visor, which means the developers looked at their own HUD, felt nothing, and shipped it. So I did the thing I do when a tool insults me to my face: I built a better one and left theirs running out of spite. The helmet keeps the flight and targeting glass. Everything else moves to a panel on the desk, and my canopy goes back to being a window instead of a goddamn pop-up ad.

Also I'm recording this stuff, and a real glass cockpit glowing on a second screen looks unreasonably good on camera. Sue me.

---

## 📦 Get it (on the gaming PC)

### The lazy way — Thunderstore

It's on [**Thunderstore**](https://thunderstore.io/c/nuclear-option/p/RyanEngineering/GlassPanel/). Hit install in your mod manager, let it drag in BepInEx for you, go get a drink. Launch the game once so the plugin opens its port, then jump to [**Run the panel**](#-run-the-panel) below.

### The one-liner — standalone installer

No mod manager, no ceremony. This finds your install, drops in BepInEx + the plugin, opens the port, and gets the hell out of the way. PowerShell:

```powershell
irm https://raw.githubusercontent.com/patrickryan01/no-glass-panel/main/install.ps1 | iex
```

### The by-hand way — for the trust issues

Grab `GlassPanel.dll` from the [latest release](https://github.com/patrickryan01/no-glass-panel/releases/latest), confirm [BepInEx](https://docs.bepinex.dev) (5.x, Mono x64) is living in the game folder, and drop the DLL into `Nuclear Option\BepInEx\plugins\`. Read the code first if it helps you sleep. I would.

The **laptop needs nothing.** It's a browser pointed at the gaming PC (`http://<gaming-pc-ip>:8787`). That's the entire client install. If that feels too easy, that's a you problem.

## 🖥️ Run the panel

```
web/index.html  →  open it in any browser
```

That's the whole thing. It boots up flying a **fake** Nuclear Option sortie so you can watch every needle move without touching the game — a frameless F-35-style HMD horizon (airspeed in **knots + km/h**, altitude in **feet + meters**, Mach, G, AoA), framed **Map** and **Radar** MFDs, an **RWR**, a **meatball / AoA** approach indexer, the full **stores** loadout, an airframe-labeled **damage** silhouette, the **mission objectives** checklist, and a **comms** window you can type into. It runs on Canvas and a bad attitude. Works on a phone or tablet too, because there's no reason it shouldn't.

The `SIM` pill flips to `LIVE` the second the mod connects and starts handing it real numbers. If it never flips, the mod isn't running or your firewall ate the port — check both before you file anything.

---

## 🧠 How it works

```
  Nuclear Option (Unity/Mono)
        │
        │  BepInEx plugin reads ownship telemetry every frame
        ▼
  Glass Panel Mod  ──►  WebSocket (JSON, ~30 Hz)  ──►  Browser panel on the laptop
   (this repo)                over your LAN              (web/index.html)
```

Nuclear Option is a Mono Unity game, which is a polite way of saying the entire game state is sitting right there for anyone with a C# compiler and no impulse control. That's not a hunch — [NOBlackBox](https://github.com/KopterBuzz/NOBlackBox) already yanks true airspeed, Mach, AoA, AGL, gear, radar mode, and raw stick/throttle out of this exact game to feed Tacview. If it can be read for a flight recorder, it can be read for a glass panel. Same data, different appetite.

The panel is deliberately stupid. It renders whatever telemetry it's handed and hallucinates a simulation when nobody's talking to it. One data contract, two producers — the sim and the mod. Swap the source, the panel doesn't notice or care. Every symbol the mod reads is **verified against the actual game assembly.** Nothing on this panel is a guess. If I couldn't prove where a number came from, it didn't get drawn.

---

## 📡 The telemetry contract

The panel eats one JSON object, `applyTelemetry(obj)`. Send any subset ~30 times a second; missing keys hold their last value. Full field list, units, and ranges live in **[docs/TELEMETRY.md](docs/TELEMETRY.md)**. That document is the seam between the two halves of this thing — the mod's only job is to fill it out honestly, and it does.

---

## 🗺️ Where it's at

Build history and definitions-of-done live in **[ROADMAP.md](ROADMAP.md)**. Everything below is fed by **verified game reads. No guessing. Not once.**

**Live and flying:**
- Full multi-MFD panel — Map + Radar, frameless HMD horizon, RWR, meatball, stores, data band. Mobile/tablet responsive.
- Real flight telemetry — airspeed (kt + km/h), altitude (ft + m), Mach, G, AoA, V/S, heading, radar alt, fuel, engine thrust, full loadout, live HOTAS inputs.
- **Aircraft-aware damage** — a silhouette labeled with your actual airframe that lights up the exact wing, nose, or tail some bastard just shot off.
- **RWR** — radars painting you *and* missiles in the air, before they become your problem.
- **Datalink** — a Link-16-style shared track picture off the faction HQ, drawn distinct from your own sensors.
- **Nav / RTB** — bearing and range to your nearest airbase, drawn as a steer line, for when the jet's on fire and you'd like to land the parts that are left.
- **Mission objectives** — the current mission's checklist, live status, over the map. Green check, red X, no ambiguity.
- **Bi-directional comms** — type into game chat from the panel; game chat comes back to it.
- **Touchscreen control** — cycle weapons, drop gear, pop flares off a tablet.
- **HMD declutter** — optionally strangle the weapon/ammo block on the in-game visor. The original sin, corrected.
- **OBS overlay** (`?overlay`) and **per-device views** (`?view=hud|map|radar|rwr`).

**On the board:**
- Whatever you break. File it below.

---

## 🧰 Stack

- **Panel:** HTML + Canvas + vanilla JS. Zero dependencies. Zero build. Ships like a brick through a window.
- **Mod:** C# on BepInEx (Mono). Reads the game, serves a WebSocket, keeps its mouth shut.

## 🧪 Beta — break it, tell me

This is **beta**. It's live, it flies, and it will absolutely still do something stupid at the worst possible moment, because that's what beta means. If a readout lies, a module renders like a ransom note, or you want a gauge that isn't there — I want to know before I find out the hard way at 400 knots.

- 🐛 **Bugs** → open an [issue](https://github.com/patrickryan01/no-glass-panel/issues/new/choose). Tell me the aircraft, **what the panel showed vs. what the game showed**, and paste `BepInEx\LogOutput.log` if it never loaded. "It's broken" is not a bug report, it's a horoscope.
- 💬 **Ideas, questions, show off your battlestation** → [Discussions](https://github.com/patrickryan01/no-glass-panel/discussions).
- 🔧 **Pull requests welcome** → see [CONTRIBUTING.md](CONTRIBUTING.md). Two-space indent. This is not negotiable and I will not be taking questions.

---

## ⚖️ License

MIT. See [LICENSE](LICENSE). Do whatever you want with it — fork it, sell it, print it out and eat it. It's a HUD panel for a video game about dropping nukes. The in-game consequences, however, remain entirely your fault.

---

*Built in Cleveland, somewhere between a White Monster and a respawn timer. If the panel saved your life, it didn't — you're still going to fly it into a mountain eventually. Everyone does.*
