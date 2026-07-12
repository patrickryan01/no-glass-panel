# ☢️ NUCLEAR OPTION — GLASS PANEL

**Some genius decided my canopy should double as a billboard. I disagreed, in C#.**

![status](https://img.shields.io/badge/status-in%20development%20·%20not%20released-ffb020?style=flat-square)
![license](https://img.shields.io/badge/license-MIT-39d6ff?style=flat-square)
![stack](https://img.shields.io/badge/stack-vanilla%20JS%20%2B%20C%23%2FBepInEx-79f0cf?style=flat-square)
![deps](https://img.shields.io/badge/node__modules-0-4ee08c?style=flat-square)
![fuel](https://img.shields.io/badge/fueled%20by-White%20Monster-white?style=flat-square)

> **⚠️ Status: in development — not working end-to-end yet, and not on Thunderstore.**
> The mod reads real telemetry out of the game (verified live), but the browser panel does
> **not** yet reliably display it. Earlier claims that this "works" were premature. Nothing
> is published or installable. See [`.claude/workflows/glass-panel-status.md`](.claude/workflows/glass-panel-status.md)
> for exactly what works, what doesn't, and what's left.

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

**There's no release yet.** It is not on Thunderstore (a build was submitted and *rejected*), there's no tagged DLL to grab, and the mod isn't confirmed working end-to-end. The only way to run it today is to **build the mod from source** — see [CONTRIBUTING.md](CONTRIBUTING.md). This section will get real install instructions when it actually works.

The **panel half** (the browser page) does run standalone right now — see below — it just flies a simulation until the mod side is finished.

## 🖥️ Run the panel

```
web/index.html  →  open it in any browser
```

That's the whole thing. It boots up flying a **fake** Nuclear Option sortie so you can watch every needle move without touching the game — a frameless F-35-style HMD horizon (airspeed in **knots + km/h**, altitude in **feet + meters**, Mach, G, AoA), framed **Map** and **Radar** MFDs, an **RWR**, a **meatball / AoA** approach indexer, the full **stores** loadout, an airframe-labeled **damage** silhouette, the **mission objectives** checklist, and a **comms** window you can type into. It runs on Canvas and a bad attitude. Works on a phone or tablet too, because there's no reason it shouldn't.

The `SIM` pill is *meant* to flip to `LIVE` the second the mod connects and starts handing it real numbers. The mod side does stream real numbers (verified) — but wiring that cleanly into the panel's live display is the part still being finished, so treat the standalone page as a demo for now.

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

Full, honest status lives in **[.claude/workflows/glass-panel-status.md](.claude/workflows/glass-panel-status.md)**. The short version:

**Verified working (mod side, confirmed in a live game):**
- The mod resolves the local aircraft in single-player and **streams valid telemetry** — real airspeed, altitude, Mach, G, AoA, V/S, heading, radar alt, fuel, engine thrust, loadout — over a stable WebSocket. Measured: 720 valid frames in 30s, no drops.
- Every game read is **verified against the actual game assembly** (decompiled), cross-checked with [NOBlackBox](https://github.com/KopterBuzz/NOBlackBox). That part is not a guess.

**Built but NOT yet confirmed end-to-end:**
- The browser panel **displaying** that live data. It renders the demo fine; on real (larger) aircraft data the render path still needs fixing before it reliably shows `LIVE`. This is the current blocker.
- Everything downstream that depends on the panel showing live data — damage silhouette, RWR, datalink, nav/RTB, objectives, comms, touchscreen control, OBS/per-device views — exists in code but is **unverified in the live loop.**

**On the board:**
- Fix the panel's live render (the one thing between "streams data" and "actually usable").
- Understand and resolve the Thunderstore rejection before any re-submit.

---

## 🧰 Stack

- **Panel:** HTML + Canvas + vanilla JS. Zero dependencies. Zero build. Ships like a brick through a window.
- **Mod:** C# on BepInEx (Mono). Reads the game, serves a WebSocket, keeps its mouth shut.

## 🧪 In development — not done yet

This is **pre-release and not working end-to-end.** The mod streams real telemetry; the panel doesn't reliably show it yet. Don't expect to install this and fly with it today — expect to build it from source and help finish it. When it actually works at 400 knots, this section changes its tune.

- 🐛 **Bugs** → open an [issue](https://github.com/patrickryan01/no-glass-panel/issues/new/choose). Tell me the aircraft, **what the panel showed vs. what the game showed**, and paste `BepInEx\LogOutput.log` if it never loaded. "It's broken" is not a bug report, it's a horoscope.
- 💬 **Ideas, questions, show off your battlestation** → [Discussions](https://github.com/patrickryan01/no-glass-panel/discussions).
- 🔧 **Pull requests welcome** → see [CONTRIBUTING.md](CONTRIBUTING.md). Two-space indent. This is not negotiable and I will not be taking questions.

---

## ⚖️ License

MIT. See [LICENSE](LICENSE). Do whatever you want with it — fork it, sell it, print it out and eat it. It's a HUD panel for a video game about dropping nukes. The in-game consequences, however, remain entirely your fault.

---

*Built in Cleveland, somewhere between a White Monster and a respawn timer. If the panel saved your life, it didn't — you're still going to fly it into a mountain eventually. Everyone does.*
