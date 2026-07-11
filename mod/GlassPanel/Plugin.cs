using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;

namespace GlassPanel
{
    [BepInPlugin(GUID, "NO Glass Panel", "1.1.0")]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ai.fireballz.noglasspanel";

        internal static ManualLogSource Log;

        private MiniServer _server;
        private TelemetryReader _reader;
        private float _accum;
        private float _interval = 1f / 30f;

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
            _server.Start();

            Log.LogInfo($"Glass Panel up. On the laptop, open  http://<this-pc-ip>:{port}");
        }

        private void Update()
        {
            if (_server == null) return;

            _accum += Time.unscaledDeltaTime;
            if (_accum < _interval) return;
            _accum = 0f;

            if (_server.ClientCount == 0) return;
            string json = _reader.BuildFrame();
            if (json != null) _server.Broadcast(json);
        }

        private void OnDestroy() => _server?.Stop();

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
