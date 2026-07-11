using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using NuclearOption.Chat;
using NuclearOption.Networking;

namespace GlassPanel
{
    // Bi-directional chat: buffers incoming game chat (captured via Harmony) for the
    // panel to display, and sends outgoing chat typed from the panel into the game.
    internal static class ChatBridge
    {
        private struct Line { public string who; public string msg; }
        private static readonly List<Line> _lines = new List<Line>();
        private static readonly object _lock = new object();
        private const int MAX = 40;

        public static void Add(string who, string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            lock (_lock)
            {
                _lines.Add(new Line { who = who ?? "", msg = msg });
                if (_lines.Count > MAX) _lines.RemoveAt(0);
            }
        }

        public static string BuildJson()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _lines.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"who\":\"").Append(Esc(_lines[i].who))
                      .Append("\",\"msg\":\"").Append(Esc(_lines[i].msg)).Append("\"}");
                }
                return sb.ToString();
            }
        }

        private static string Esc(string s) => s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // Call on the main thread only (drained from Plugin.Update).
        public static void Send(string text, bool allChat)
        {
            try
            {
                if (!string.IsNullOrEmpty(text) && ChatManager.CanSend(text, false, false))
                    ChatManager.SendChatMessage(text, allChat);
            }
            catch { }
        }
    }

    // Incoming player messages.
    [HarmonyPatch(typeof(ChatManager), "UserCode_TargetReceiveMessage_1307761090")]
    internal static class Patch_ReceiveMessage
    {
        static void Postfix(string message, Player player, bool allChat)
        {
            string who = "?";
            try { if (player != null) who = player.PlayerName; } catch { }
            ChatBridge.Add(who, message);
        }
    }

    // Incoming server/system messages.
    [HarmonyPatch(typeof(ChatManager), "UserCode_RpcServerMessage_1244201393")]
    internal static class Patch_ServerMessage
    {
        static void Postfix(string message) => ChatBridge.Add("SERVER", message);
    }
}
