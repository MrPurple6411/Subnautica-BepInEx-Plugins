using FMOD;
using HarmonyLib;
using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BuildingTweaks.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class Player_Update_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            PlayerTool heldTool = Inventory.main.GetHeldTool();

            bool builderCheck = heldTool != null && heldTool.pickupable.GetTechType() == TechType.Builder;
            
            string msg2 = $"Full Override = {Main.FullOverride}";
            ErrorMessage._Message emsg2 = ErrorMessage.main.GetExistingMessage(msg2);

            if (DevConsole.instance != null && !DevConsole.instance.state && (builderCheck))
            {

                if (Input.GetKeyDown(KeyCode.G))
                {
                    Main.FullOverride = !Main.FullOverride;
                    msg2 = $"Full Override = {Main.FullOverride}";

                }

                if (emsg2 != null)
                {
                    emsg2.messageText = msg2;
                    emsg2.entry.text = msg2;

                    if(emsg2.timeEnd <= Time.time + 1f)
                        emsg2.timeEnd += Time.deltaTime;
                }
                else
                    ErrorMessage.AddMessage(msg2);
            }
            else if( Main.FullOverride)
            {
                Main.FullOverride = false;

                if (emsg2 != null)
                    emsg2.timeEnd = Time.time;
            }

            SubRoot currentSubRoot = __instance.GetCurrentSub();
            if (currentSubRoot != null && currentSubRoot is BaseRoot && __instance.playerController.velocity.y < -20f)
            {
                RespawnPoint componentInChildren = currentSubRoot.gameObject.GetComponentInChildren<RespawnPoint>();
                if (componentInChildren)
                {
                    __instance.SetPosition(componentInChildren.GetSpawnPosition());
                    return;
                }
            }

            EscapePod escapePod = __instance.currentEscapePod;
            if(escapePod != null && __instance.playerController.velocity.y < -20f)
            {
                __instance.SetPosition(escapePod.playerSpawn.transform.position, escapePod.playerSpawn.transform.rotation);
                return;
            }
        }
    }
}
