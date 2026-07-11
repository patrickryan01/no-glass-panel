using System.Collections.Generic;
using UnityEngine;

namespace GlassPanel
{
    // Radars currently painting the local aircraft. Fed by Aircraft.onRadarWarning
    // (subscribed in Plugin), expired after a short TTL. This is the "being tracked"
    // half of the RWR, alongside the missile-launch warnings.
    internal static class RadarWarnTracker
    {
        private static readonly Dictionary<Radar, float> _hits = new Dictionary<Radar, float>();
        private static readonly object _lock = new object();
        private const float TTL = 4f;

        public static void Mark(Radar r)
        {
            if (r == null) return;
            lock (_lock) { _hits[r] = Time.time; }
        }

        public static List<Radar> Active()
        {
            var live = new List<Radar>();
            lock (_lock)
            {
                float now = Time.time;
                List<Radar> dead = null;
                foreach (var kv in _hits)
                {
                    if (kv.Key == null || now - kv.Value > TTL) { (dead ?? (dead = new List<Radar>())).Add(kv.Key); }
                    else live.Add(kv.Key);
                }
                if (dead != null) foreach (Radar d in dead) _hits.Remove(d);
            }
            return live;
        }
    }
}
