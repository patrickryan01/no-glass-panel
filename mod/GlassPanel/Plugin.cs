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
    [BepInPlugin(GUID, "NO Glass Panel", "1.3.0")]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ai.fireballz.noglasspanel";

        internal static ManualLogSource Log;

        private MiniServer _server;
        private TelemetryReader _reader;
        private float _accum;
        private float _interval = 1f / 30f;
        private readonly Queue<string> _inbox = new Queue<string>();
        private readonly object _inboxLock = new object();

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

        // {"t":"chat","all":true,"text":"..."} — text is the last field.
        private static void HandlePanelMessage(string json)
        {
            if (json == null || json.IndexOf("\"chat\"", System.StringComparison.Ordinal) < 0) return;
            bool all = json.IndexOf("\"all\":true", System.StringComparison.Ordinal) >= 0;
            int i = json.IndexOf("\"text\":\"", System.StringComparison.Ordinal);
            if (i < 0) return;
            i += 8;
            int j = json.LastIndexOf('"');
            if (j <= i) return;
            string text = json.Substring(i, j - i).Replace("\\\"", "\"").Replace("\\\\", "\\");
            ChatBridge.Send(text, all);
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
