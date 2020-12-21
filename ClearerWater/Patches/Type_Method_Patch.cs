using HarmonyLib;
using UnityEngine;

namespace ClearerWater.Patches
{
    [HarmonyPatch(typeof(WaterscapeVolume.Settings), nameof(WaterscapeVolume.Settings.GetExtinctionAndScatteringCoefficients))]
    public static class WaterscapeVolume_Settings_Patches
    {
        [HarmonyPrefix]
        public static bool Patch_GetExtinctionAndScatteringCoefficients(WaterscapeVolume.Settings __instance, ref Vector4 __result)
        {
            var t = __instance;
            float d = t.murkiness / 190f;
            Vector3 vector = t.absorption + t.scattering * Vector3.one;
            __result = new Vector4(vector.x, vector.y, vector.z, t.scattering) * d;
            return false;
        }
    }
}
