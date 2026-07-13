using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GlassPanel
{
    [BepInPlugin(GUID, "NO Glass Panel", "1.9.0")]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ai.fireballz.noglasspanel";

        internal static ManualLogSource Log;
        internal static bool HideWeaponHMD;

        private MiniServer _server;
        private TelemetryReader _reader;
        private float _accum;
        private float _diagTimer;
        private float _interval = 1f / 30f;
        private readonly Queue<string> _inbox = new Queue<string>();
        private readonly object _inboxLock = new object();
        private Aircraft _subAircraft;
        private System.Action<Aircraft.OnRadarWarning> _radarHandler;

        private void Awake()
        {
            Log = Logger;

            // Nuclear Option destroys BepInEx's manager object during startup, which would
            // take this plugin (and its server) with it. Force-hide + keep it alive — the
            // same workaround NOBlackBox uses to survive in this game.
            GameObject manager = Chainloader.ManagerObject;
            if (manager != null)
            {
                manager.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(manager);
            }

            int port = Config.Bind("Server", "Port", 8787,
                "TCP port serving the panel page (HTTP) and the live feed (WebSocket) on the same port.").Value;
            float hz = Config.Bind("Server", "UpdateHz", 30f,
                "Telemetry frames per second pushed to connected panels.").Value;
            _interval = 1f / Mathf.Max(1f, hz);
            bool lanMode = Config.Bind("Server", "LanMode", false,
                "Set true to bind the server to all network interfaces (0.0.0.0) so you can view the panel " +
                "from a tablet or second PC on your LAN. Default (false) binds to localhost only " +
                "(127.0.0.1) — the safest option for single-machine use.").Value;
            System.Net.IPAddress bindAddr = lanMode ? System.Net.IPAddress.Any : System.Net.IPAddress.Loopback;
            HideWeaponHMD = Config.Bind("HMD", "HideWeaponAmmo", false,
                "Hide the weapon/ammo indicator on the in-game HMD (it lives on the panel instead).").Value;

            _reader = new TelemetryReader();
            _server = new MiniServer(port, bindAddr, LoadPanelHtml());
            _server.OnMessage = OnPanelMessage;
            _server.Start();

            // Harmony patches capture incoming game chat for the panel. Non-fatal:
            // sending chat from the panel works even if this fails to apply.
            try { new Harmony(GUID).PatchAll(); }
            catch (System.Exception ex) { Log.LogWarning("chat receive patch skipped: " + ex.Message); }

            Log.LogInfo($"Glass Panel up. On the laptop, open  http://<this-pc-ip>:{port}");
        }

        private void Update()
        {
            if (_server == null) return;

            // Drain panel->game messages on the main thread (chat send must run here).
            lock (_inboxLock) { while (_inbox.Count > 0) HandlePanelMessage(_inbox.Dequeue()); }
            EnsureRadarSub();

            _accum     += Time.unscaledDeltaTime;
            _diagTimer += Time.unscaledDeltaTime;
            if (_accum < _interval) return;
            _accum = 0f;

            // Diagnostic: log every 5s when no data flows (even without browser open).
            string json = _reader.BuildFrame();
            if (json != null)
            {
                _diagTimer = 0f;
                if (_server.ClientCount > 0) _server.Broadcast(json);
            }
            else if (_diagTimer > 5f)
            {
                _diagTimer = 0f;
                try
                {
                    bool   missionRunning = MissionManager.IsRunning;
                    var    cam   = SceneSingleton<CameraStateManager>.i;
                    var    fhud  = SceneSingleton<FlightHud>.i;
                    bool   gla   = GameManager.GetLocalAircraft(out Aircraft glaAc);
                    var    fhAcField = typeof(FlightHud).GetField("aircraft",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Aircraft fhAc = fhAcField?.GetValue(fhud) as Aircraft;
                    Unit     camUnit = cam?.followingUnit;
                    Log.LogInfo($"[diag] " +
                        $"Mission={missionRunning} | " +
                        $"GameState={GameManager.gameState} | " +
                        $"CamFollowing={(camUnit == null ? "null" : camUnit.name + "(" + camUnit.GetType().Name + ")")} | " +
                        $"FlightHud={(fhAc == null ? "null" : fhAc.name)} | " +
                        $"GetLocalAircraft={gla}/{(glaAc == null ? "null" : glaAc.name)} | " +
                        $"playerLookup.Count={UnitRegistry.playerLookup?.Count ?? -1}");
                }
                catch (System.Exception ex) { Log.LogWarning("[diag] ex: " + ex.Message); }
            }
        }

        private void OnDestroy() => _server?.Stop();

        private void OnPanelMessage(string msg)
        {
            lock (_inboxLock) { if (_inbox.Count < 32) _inbox.Enqueue(msg); }
        }

        // Keep subscribed to the local aircraft's radar-warning event (re-subscribes on respawn).
        private void EnsureRadarSub()
        {
            try
            {
                // Use the same 3-path resolver TelemetryReader uses.
                // GameManager.GetLocalAircraft() returns null in singleplayer/host
                // (networking _localPlayer never fires), so the radar subscription
                // was silently dead in SP. ResolveLocalAircraft() handles all three cases.
                Aircraft ac = TelemetryReader.ResolveLocalAircraft();
                if (ReferenceEquals(ac, _subAircraft)) return;
                if (_subAircraft != null && _radarHandler != null) _subAircraft.onRadarWarning -= _radarHandler;
                _subAircraft = ac;
                if (ac != null)
                {
                    if (_radarHandler == null) _radarHandler = w => RadarWarnTracker.Mark(w.radar);
                    ac.onRadarWarning += _radarHandler;
                }
            }
            catch { }
        }

        // Panel -> game. Chat: {"t":"chat","all":true,"text":"..."}. Command: {"t":"cmd","a":"gear"}.
        private static void HandlePanelMessage(string json)
        {
            if (json == null) return;
            if (json.IndexOf("\"cmd\"", System.StringComparison.Ordinal) >= 0) { HandleCommand(json); return; }
            if (json.IndexOf("\"chat\"", System.StringComparison.Ordinal) < 0) return;
            bool all = json.IndexOf("\"all\":true", System.StringComparison.Ordinal) >= 0;
            int i = json.IndexOf("\"text\":\"", System.StringComparison.Ordinal);
            if (i < 0) return;
            i += 8;
            int j = json.LastIndexOf('"');
            if (j <= i) return;
            string text = json.Substring(i, j - i).Replace("\\\"", "\"").Replace("\\\\", "\\");
            ChatBridge.Send(text, all);
        }

        // Touchscreen control from the panel — all verified aircraft methods.
        private static void HandleCommand(string json)
        {
            string a = Extract(json, "\"a\":\"");
            if (a == null) return;
            // Use the same resolver as telemetry — GetLocalAircraft fails in SP/host.
            Aircraft ac = TelemetryReader.ResolveLocalAircraft();
            if (ac == null) return;
            try
            {
                switch (a)
                {
                    case "gear": ac.SetGear(!ac.gearDeployed); break;
                    case "wpn+": if (ac.weaponManager != null) ac.weaponManager.NextWeaponStation(); break;
                    case "wpn-": if (ac.weaponManager != null) ac.weaponManager.PreviousWeaponStation(); break;
                    case "cm": if (ac.countermeasureManager != null) ac.countermeasureManager.PopFlares(); break;
                }
            }
            catch { }
        }

        private static string Extract(string json, string marker)
        {
            int i = json.IndexOf(marker, System.StringComparison.Ordinal);
            if (i < 0) return null;
            i += marker.Length;
            int j = json.IndexOf('"', i);
            return j > i ? json.Substring(i, j - i) : null;
        }

        private static string LoadPanelHtml()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("GlassPanel.index.html"))
            {
                if (s == null) return "<!doctype html><h1>panel resource missing from mod build</h1>";
                using (var r = new StreamReader(s)) return r.ReadToEnd();
            }
        }
    }

    // ── Harmony patch: Aircraft.SetLocalSim ───────────────────────────────────────────
    // Called from Aircraft.InitializeUnit (line 57459) for EVERY aircraft the host simulates.
    // In SP the player is host, so ALL aircraft get SetLocalSim(true) — we must filter.
    // Only capture when the aircraft is the local player's: Player != null && IsLocalPlayer.
    // When localSim=false our jet died/despawned → clear the cache so BuildFrame stops
    // reading a destroyed object and the panel cleanly falls back to SIM.
    [HarmonyPatch(typeof(Aircraft), "SetLocalSim")]
    static class LocalSimPatch
    {
        [HarmonyPostfix]
        static void Postfix(Aircraft __instance, bool localSim)
        {
            try
            {
                if (localSim)
                {
                    // Guard: only the local player's aircraft has Player.IsLocalPlayer == true.
                    var p = __instance.Player;
                    if (p != null && p.IsLocalPlayer)
                    {
                        TelemetryReader.LocalAircraft = __instance;
                        Plugin.Log.LogInfo("[GlassPanel] LOCAL SIM AIRCRAFT: " + __instance.name);
                    }
                }
                else if (TelemetryReader.LocalAircraft == __instance)
                {
                    TelemetryReader.LocalAircraft = null;
                    Plugin.Log.LogInfo("[GlassPanel] LOCAL SIM CLEARED");
                }
            }
            catch { /* IsLocalPlayer not available — fall through to SetupLocalPlayerAndUI */ }
        }
    }

    // ── Harmony patch: Aircraft.SetupLocalPlayerAndUI ─────────────────────────────────
    // Secondary hook — only fires in MP (where Player != null). Kept as belt-and-suspenders.
    [HarmonyPatch(typeof(Aircraft), "SetupLocalPlayerAndUI")]
    static class LocalAircraftPatch
    {
        [HarmonyPrefix]
        static void Prefix(Aircraft __instance)
        {
            TelemetryReader.LocalAircraft = __instance;
            Plugin.Log.LogInfo("[GlassPanel] LOCAL AIRCRAFT (MP): " + __instance.name);
        }
    }
}
