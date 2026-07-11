using HarmonyLib;

namespace GlassPanel
{
    // Optionally hide the weapon/ammo indicator on the in-game HMD (config-gated,
    // off by default). The panel carries that info now, so the visor can stay clean.
    [HarmonyPatch(typeof(WeaponIndicator), "Show")]
    internal static class Patch_HideWeaponIndicator
    {
        static void Prefix(ref bool arg)
        {
            if (Plugin.HideWeaponHMD) arg = false;
        }
    }
}
