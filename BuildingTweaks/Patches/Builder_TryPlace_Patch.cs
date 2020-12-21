using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BuildingTweaks.Patches
{
    [HarmonyPatch(typeof(Builder), nameof(Builder.TryPlace))]
    internal class Builder_TryPlace_Patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
            bool found = false;
            List<OpCode> opCodes = new List<OpCode>() { OpCodes.Ldloc_2, OpCodes.Ldloc_3 };

            for (int i = 0; i < instructions.Count() - 2; i++)
            {
                CodeInstruction currentInstruction = codeInstructions[i];
                CodeInstruction secondInstruction = codeInstructions[i + 1];
                CodeInstruction thirdInstruction = codeInstructions[i + 2];

                if (opCodes.Contains(currentInstruction.opcode) && secondInstruction.opcode == OpCodes.Callvirt && thirdInstruction.opcode == OpCodes.Dup)
                {
                    codeInstructions[i + 1] = new CodeInstruction(OpCodes.Call, typeof(Builder_TryPlace_Patch).GetMethod(nameof(Builder_TryPlace_Patch.SetParent)));
                    found = true;
                    break;
                }
                continue;
            }

            if (found is false)
                Main.main.log.LogError($"Cannot find patch location in Builder.TryPlace");
            else
                Main.main.log.LogInfo("Transpiler for Builder.TryPlace completed");

            return codeInstructions.AsEnumerable();
        }

        public static Transform SetParent(GameObject builtObject)
        {
            LargeWorldEntity largeWorldEntity = builtObject.GetComponent<LargeWorldEntity>();
            if (largeWorldEntity is null)
            {
                largeWorldEntity = builtObject.AddComponent<LargeWorldEntity>();
                largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Medium;
            }
            else if (builtObject.name.Contains("Transmitter"))
            {
                largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Global;
            }

            return builtObject.transform;
        }
    }

}
