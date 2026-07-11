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
    [BepInPlugin(GUID, "NO Glass Panel", "1.5.0")]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ai.fireballz.noglasspanel";

        internal static ManualLogSource Log;
        internal static bool HideWeaponHMD;

        private MiniServer _server;
        private TelemetryReader _reader;
        private float _accum;
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
            HideWeaponHMD = Config.Bind("HMD", "HideWeaponAmmo", false,
                "Hide the weapon/ammo indicator on the in-game HMD (it lives on the panel instead).").Value;

            _reader = new TelemetryReader();
            _server = new MiniServer(port, LoadPanelHtml());
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

            _accum += Time.unscaledDeltaTime;
            if (_accum < _interval) return;
            _accum = 0f;

            if (_server.ClientCount == 0) return;
            string json = _reader.BuildFrame();
            if (json != null) _server.Broadcast(json);
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
                GameManager.GetLocalAircraft(out Aircraft ac);
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
            if (!GameManager.GetLocalAircraft(out Aircraft ac) || ac == null) return;
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
}
