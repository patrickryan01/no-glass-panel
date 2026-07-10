using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace GlassPanel
{
    [BepInPlugin(GUID, "NO Glass Panel", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ai.fireballz.noglasspanel";

        internal static ManualLogSource Log;

        private MiniServer _server;
        private TelemetryReader _reader;
        private ConfigEntry<int> _port;
        private ConfigEntry<float> _hz;
        private float _accum;

        private void Awake()
        {
            Log = Logger;
            _port = Config.Bind("Server", "Port", 8787,
                "TCP port serving the panel page (HTTP) and the live feed (WebSocket) on the same port.");
            _hz = Config.Bind("Server", "UpdateHz", 30f,
                "Telemetry frames per second pushed to connected panels.");

            _reader = new TelemetryReader();
            _server = new MiniServer(_port.Value, LoadPanelHtml());
            _server.Start();

            Log.LogInfo($"Glass Panel up. On the laptop, open  http://<this-pc-ip>:{_port.Value}");
        }

        private void Update()
        {
            if (_server == null) return;
            _accum += Time.unscaledDeltaTime;
            float interval = 1f / Mathf.Max(1f, _hz.Value);
            if (_accum < interval) return;
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
