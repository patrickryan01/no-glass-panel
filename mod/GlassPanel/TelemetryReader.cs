using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace GlassPanel
{
    // Reads the local aircraft each frame and serializes the telemetry contract
    // (docs/TELEMETRY.md) to JSON by hand. Every read below is a verified game
    // member — see docs/GAME_SYMBOLS.md. No guessing.
    internal class TelemetryReader
    {
        private const float MS_TO_KN = 1.943844f;
        private const float M_TO_FT = 3.28084f;
        private const float MS_TO_FTMIN = 196.8504f;
        private const float RAD_TO_DEG = 57.29578f;

        // Returns a JSON frame, or null when there is no local aircraft (menu/loading).
        public string BuildFrame()
        {
            if (!GameManager.GetLocalAircraft(out Aircraft ac) || ac == null)
                return null;

            Transform t = ac.transform;
            Vector3 vel = (ac.cockpit != null && ac.cockpit.rb != null) ? ac.cockpit.rb.velocity : Vector3.zero;
            float speed = ac.speed; // m/s, true airspeed

            // Attitude — same adjustment NOBlackBox uses.
            float pitchRaw = t.eulerAngles.x, rollRaw = t.eulerAngles.z;
            float pitch = pitchRaw > 180f ? 360f - pitchRaw : -pitchRaw;
            float roll = rollRaw > 180f ? 360f - rollRaw : -rollRaw;
            float hdg = t.eulerAngles.y;

            // Angle of attack from the cockpit rigidbody's velocity in body axes.
            float aoa = 0f;
            if (ac.cockpit != null)
            {
                Vector3 local = ac.cockpit.transform.InverseTransformDirection(vel);
                aoa = Mathf.Atan2(local.y, local.z) * -RAD_TO_DEG;
            }

            ControlInputs inp = ac.GetInputs();

            // IAS from TAS via ISA density ratio (troposphere).
            float altM = t.position.GlobalY();
            float sigma = Mathf.Pow(Mathf.Max(0.05f, 1f - 2.25577e-5f * Mathf.Max(0f, altM)), 4.2561f);

            var p = new List<string>(28);
            p.Add(Num("tas", speed * MS_TO_KN));
            p.Add(Num("ias", speed * Mathf.Sqrt(sigma) * MS_TO_KN));
            p.Add(Num("mach", speed / 340f));
            p.Add(Num("alt", t.position.GlobalY() * M_TO_FT));
            p.Add(Num("agl", Mathf.Max(0f, ac.radarAlt) * M_TO_FT));
            p.Add(Num("vs", vel.y * MS_TO_FTMIN));
            p.Add(Num("hdg", hdg));
            p.Add(Num("pitch", pitch));
            p.Add(Num("roll", roll));
            p.Add(Num("aoa", aoa));
            p.Add(Num("g", ac.gForce));
            p.Add(Num("engine", EngineThrustKN(ac)));
            p.Add(Num("throttle", inp.throttle));
            p.Add(Bool("gear", ac.gearDeployed));
            p.Add(Num("fuelKg", ac.GetFuelQuantity()));
            p.Add("\"inputs\":{" + Num("pitch", inp.pitch) + "," + Num("roll", inp.roll) + "," +
                  Num("yaw", inp.yaw) + "," + Num("throttle", inp.throttle) + "}");
            int sel;
            p.Add("\"weapons\":[" + BuildLoadout(ac, out sel) + "]");
            p.Add(Int("weaponIndex", sel));

            int flares = 0, chaff = 0, flaresMax = 0, chaffMax = 0;
            ReadCountermeasures(ac, ref flares, ref chaff, ref flaresMax, ref chaffMax);
            p.Add(Int("flares", flares));
            p.Add(Int("chaff", chaff));
            p.Add(Int("flaresMax", flaresMax <= 0 ? 1 : flaresMax));
            p.Add(Int("chaffMax", chaffMax <= 0 ? 1 : chaffMax));

            p.Add("\"contacts\":[" + BuildContacts(ac) + "]");
            p.Add("\"rwr\":[" + BuildRWR(ac) + "]");
            p.Add(BuildDamage(ac));
            p.Add("\"chat\":[" + ChatBridge.BuildJson() + "]");

            return "{" + string.Join(",", p.ToArray()) + "}";
        }

        // Total engine thrust in kN across all engines.
        private static float EngineThrustKN(Aircraft ac)
        {
            try
            {
                float sum = 0f;
                if (ac.engines != null)
                    foreach (IEngine e in ac.engines) if (e != null) sum += e.GetThrust();
                return sum / 1000f;
            }
            catch { return 0f; }
        }

        // Full loadout: every weapon station, with the selected one's index.
        private static string BuildLoadout(Aircraft ac, out int selIndex)
        {
            selIndex = 0;
            try
            {
                var stations = ac.weaponStations;
                if (stations == null) return BuildSelectedWeapon(ac);
                WeaponStation cur = ac.weaponManager != null ? ac.weaponManager.currentWeaponStation : null;
                var items = new List<string>();
                foreach (WeaponStation ws in stations)
                {
                    if (ws == null || ws.WeaponInfo == null) continue;
                    string name = !string.IsNullOrEmpty(ws.WeaponInfo.weaponName) ? ws.WeaponInfo.weaponName : ws.WeaponInfo.shortName;
                    if (cur != null && ReferenceEquals(ws, cur)) selIndex = items.Count;
                    items.Add("{" + Str("name", name ?? "-") + "," + Int("ammo", ws.Ammo) + "," + Str("unit", "") + "}");
                }
                return items.Count > 0 ? string.Join(",", items.ToArray()) : BuildSelectedWeapon(ac);
            }
            catch { selIndex = 0; return BuildSelectedWeapon(ac); }
        }

        private static string BuildSelectedWeapon(Aircraft ac)
        {
            WeaponManager wm = ac.weaponManager;
            WeaponStation ws = wm != null ? wm.currentWeaponStation : null;
            if (ws == null || ws.WeaponInfo == null) return "";
            string name = !string.IsNullOrEmpty(ws.WeaponInfo.weaponName)
                ? ws.WeaponInfo.weaponName : ws.WeaponInfo.shortName;
            return "{" + Str("name", name ?? "-") + "," + Int("ammo", ws.Ammo) + "," + Str("unit", "RDS") + "}";
        }

        // CountermeasureManager keeps its stations private; read the real ammo/maxAmmo
        // via reflection and split flare vs chaff by the station's threatTypes.
        private static void ReadCountermeasures(Aircraft ac, ref int flares, ref int chaff, ref int flaresMax, ref int chaffMax)
        {
            try
            {
                CountermeasureManager cm = ac.countermeasureManager;
                if (cm == null) return;
                FieldInfo stationsField = typeof(CountermeasureManager)
                    .GetField("countermeasureStations", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!(stationsField?.GetValue(cm) is IEnumerable stations)) return;

                foreach (object st in stations)
                {
                    if (st == null) continue;
                    System.Type sty = st.GetType();
                    int ammo = (int)(sty.GetField("ammo")?.GetValue(st) ?? 0);
                    int max = (int)(sty.GetField("maxAmmo")?.GetValue(st) ?? 0);
                    var types = sty.GetField("threatTypes")?.GetValue(st) as List<string>;
                    bool isChaff = types != null && types.Exists(x => x != null && x.ToLower().Contains("radar"));
                    if (isChaff) { chaff += ammo; chaffMax += max; }
                    else { flares += ammo; flaresMax += max; }
                }
            }
            catch { /* stay silent; panel just shows zeros */ }
        }

        private static string BuildContacts(Aircraft ac)
        {
            try
            {
                WeaponManager wm = ac.weaponManager;
                List<Unit> targets = wm != null ? wm.GetTargetList() : null;
                if (targets == null) return "";
                var items = new List<string>();
                int n = 0;
                foreach (Unit u in targets)
                {
                    if (u == null || n >= 12) continue;
                    n++;
                    Vector3 d = u.transform.position - ac.transform.position;
                    float rng = d.magnitude;
                    float brg = (Mathf.Atan2(d.x, d.z) * RAD_TO_DEG + 360f) % 360f;
                    items.Add("{" + Num("brg", brg) + "," + Num("rng", rng) + "," + Str("type", "H") + "}");
                }
                return string.Join(",", items.ToArray());
            }
            catch { return ""; }
        }

        // Incoming-missile warnings from the game's MissileWarning system -> RWR launch threats.
        private static string BuildRWR(Aircraft ac)
        {
            try
            {
                MissileWarning mw = ac.GetMissileWarningSystem();
                if (mw == null || mw.knownMissiles == null) return "";
                var items = new List<string>();
                foreach (Missile m in mw.knownMissiles)
                {
                    if (m == null) continue;
                    Vector3 d = m.transform.position - ac.transform.position;
                    float brg = (Mathf.Atan2(d.x, d.z) * RAD_TO_DEG + 360f) % 360f;
                    items.Add("{" + Num("brg", brg) + "," + Str("band", "M") + "," + Int("lock", 2) + "}");
                }
                return string.Join(",", items.ToArray());
            }
            catch { return ""; }
        }

        // Damage integrity from the part-damage tracker (fraction of parts blown off).
        private static string BuildDamage(Aircraft ac)
        {
            float hull = 1f;
            try
            {
                if (ac.partDamageTracker != null)
                    hull = 1f - ac.partDamageTracker.GetDetachedRatio();
            }
            catch { }
            return "\"damage\":{" + Num("hull", hull) + "}";
        }

        // ── tiny JSON helpers, invariant culture ──
        private static string Num(string k, float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
            return "\"" + k + "\":" + v.ToString("0.###", CultureInfo.InvariantCulture);
        }
        private static string Int(string k, int v) => "\"" + k + "\":" + v.ToString(CultureInfo.InvariantCulture);
        private static string Bool(string k, bool v) => "\"" + k + "\":" + (v ? "true" : "false");
        private static string Str(string k, string v) => "\"" + k + "\":\"" + Esc(v) + "\"";
        private static string Esc(string s) => s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
