﻿using HarmonyLib;

namespace NoMask.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetScubaMaskActive))]
    public static class Player_SetScubaMaskActive_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref bool state)
        {
            state = false;
        }
    }
}
