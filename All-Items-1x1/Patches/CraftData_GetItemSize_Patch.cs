using HarmonyLib;

namespace All_Items_1x1.Patches
{
    [HarmonyPatch(typeof(CraftData), nameof(CraftData.GetItemSize))]
    public static class CraftData_GetItemSize_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Vector2int __result)
        {
            __result = new Vector2int(1, 1);
        }
    }
}
