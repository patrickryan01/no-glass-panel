# Glass Panel — Status (2026-07-12)

Honest state of the project after a long live-debugging session. Written so the next
person (or the next session) doesn't repeat the mistake that started this: **treating
"it compiles" as "it works."**

## TL;DR

- The **mod-side telemetry pipeline works** — verified in a live game today, not just by compilation.
- The **browser panel does not yet reliably display live data** — it renders the demo, and on real (larger) aircraft data the render loop was throwing and collapsing the page. That's the open bug.
- **Nothing is public.** The Thunderstore uploads (v1.8.0, v1.8.1) were **rejected** by community moderation ("Invalid submission") and never listed or installable. No user ever downloaded a broken build.
- Prior "it works / verified / live and flying" claims in the README and version cuts were **premature** and have been walked back.

## What is actually verified (measured today)

Proven with an independent .NET `ClientWebSocket` against the running mod:

- The mod resolves the local aircraft in single-player and streams valid JSON telemetry.
  - Example capture: **720 valid frames over 30s, smooth (max gap 0.07s), no drops.**
  - Real values confirmed on the wire: `tas`, `alt`, `hdg`, `fuelKg`, etc. — multiple airframes (COIN, CAS1, trainer, Fighter1).
- The WebSocket connection is now **stable** (was dropping every ~5s — see fixes below).

## Bugs found and fixed this session

1. **Single-player aircraft resolution.** `GameManager.GetLocalAircraft()` returns null in
   single-player/host because the networking `_localPlayer` (`Player.OnStartLocalPlayer`)
   never fires. Fixed in `TelemetryReader.ResolveLocalAircraft()` — falls back to
   `CombatHUD.aircraft`, then `UnitRegistry.playerLookup`. (Decompiled from the real
   `Assembly-CSharp.dll`; see `decompiled/`.)
2. **5-second connection drop (the reconnect storm).** `MiniServer.ReadHeaders` set
   `NetworkStream.ReadTimeout = 5000`, which maps to the underlying `Socket.ReceiveTimeout`
   and **persists**. A connected panel sends nothing until you type chat, so every socket's
   blocking read timed out after 5s and the mod closed the connection. Fixed with
   `sock.ReceiveTimeout = 0` in `ReadLoop`. (Ref: Microsoft `NetworkStream.ReadTimeout` docs
   / .NET reference source.)
3. **Partial WebSocket sends.** `Socket.Send(frame)` can write fewer bytes than requested;
   the code ignored the return value. Replaced with a send-all loop so frames don't desync.
4. **Panel render loop had no error guard.** One throw killed the whole `requestAnimationFrame`
   loop and collapsed the layout ("melted"). Wrapped the render calls in try/catch that logs
   `panel render error (real telemetry):` once. This makes the page crash-proof but does not
   yet *fix* the underlying render bug.

## The one open bug

**The panel does not confirmed-render live data.** With a real Fighter1, the frame is ~7KB
(vs ~3.7KB for a trainer) — much larger `weapons` / `contacts` / `datalink` arrays. A render
function throws on some real value/shape. It's now caught (no more melt), but live display
was never confirmed with human eyes in a browser before the game was closed.

### How to finish it (game must be running)

1. Launch NO, get in a jet, open `http://127.0.0.1:8787` (**not** `localhost` — the server
   binds IPv4 only; `localhost` resolves to IPv6 `::1` and won't connect).
2. Open F12 → Console. The crash-proof loop prints the exact failing error/line once.
3. Alternatively capture the real frame (`scratchpad/dumpframe.ps1`) and diff its array
   shapes against what `web/index.html` `dom()` / the render fns assume.
4. Fix the field, then **watch it render in the browser** before claiming it works.

## Thunderstore

- `RyanEngineering-GlassPanel` v1.8.1 exists but is **rejected** in the `nuclear-option`
  community — not listed, not in the mod manager, not installable as a dependency. Only the
  owning team can see the page. Reason shown: "Invalid submission."
- Do **not** publish again until the panel is verified working end-to-end AND the rejection
  reason is understood (ask the NO modding / Thunderstore Discord — the mod opens a LAN
  listening socket, which likely reads as a red flag without context).

## Gotchas for the next session

- The panel is embedded in the DLL at build time (`GlassPanel.csproj` embeds `web/index.html`).
  A panel change requires **rebuild + reinstall + game restart** to reach the browser.
- The game memory-maps the plugin DLL; you must fully exit NO before overwriting it, then
  relaunch via `steam://rungameid/2168680`.
- Don't hand-parse WebSocket frames to test — hand-rolled parsers desync and lie. Use
  `System.Net.WebSockets.ClientWebSocket` (`scratchpad/wsproper.ps1`, `wshold.ps1`).
