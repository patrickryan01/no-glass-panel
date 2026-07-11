# Contributing

It's beta and it's a HUD panel for a video game, not a spacecraft ground station. PRs, issues, and ideas all welcome — don't overthink it.

## 🐛 Bugs
Open an [issue](https://github.com/patrickryan01/no-glass-panel/issues/new/choose) with the template. Include:
- The **aircraft** you were flying
- What the **panel showed vs. what the game showed**
- `BepInEx\LogOutput.log` if the mod didn't load

## 💬 Not a bug?
Ideas, questions, setups → [Discussions](https://github.com/patrickryan01/no-glass-panel/discussions).

## 🔧 Building the mod
Needs **.NET SDK 8** and **BepInEx 5 (Mono x64)** in the game folder.

```powershell
cd mod/GlassPanel
dotnet build -c Release -p:NuclearOptionDir="<your Nuclear Option path>"
# or just: mod/build.ps1  (builds + drops the DLL into BepInEx/plugins)
```

The DLL lands in `bin/Release/net472/`.

## 🖥️ The panel
`web/index.html` — vanilla JS + Canvas. No build, no deps. Open it in a browser and it runs a simulation when nothing's feeding it. Data contract is [docs/TELEMETRY.md](docs/TELEMETRY.md); every game read is verified in [docs/GAME_SYMBOLS.md](docs/GAME_SYMBOLS.md). **Rule: no guessing at game internals — verify against the assembly (and NOBlackBox) before it ships.**

## 🎨 Style
Two-space indent. Keep the repo clean — no build junk, no IDE cruft, no `node_modules` (there are none, keep it that way). Match the voice in the docs or don't; I'm not your editor.
