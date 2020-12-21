using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RecyclingBin
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "RecyclingBin",
            AUTHOR = "MrPurple6411",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0.0";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        #endregion

        public Main()
        {
            log = Logger;
            harmony = new Harmony(GUID);
            assembly = Assembly.GetExecutingAssembly();
            modFolder = Path.GetDirectoryName(assembly.Location);
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }

        public static bool BatteryCheck(Pickupable pickupable)
        {
            TechType techType = pickupable.GetTechType();
            string techString = Language.main.GetOrFallback(techType.AsString(), techType.AsString());

            EnergyMixin energyMixin = pickupable.gameObject.GetComponentInChildren<EnergyMixin>();
            if (energyMixin != null)
            {

                GameObject gameObject = energyMixin.GetBattery();

                bool defaultCheck = false;
                if (gameObject != null)
                {
                    TechType batteryTechType = CraftData.GetTechType(gameObject);

                    defaultCheck = energyMixin.defaultBattery == batteryTechType;

                    if (defaultCheck)
                    {
                        IBattery battery = gameObject.GetComponent<IBattery>();

                        if (battery != null && battery.charge < (battery.capacity * 0.99))
                        {
                            ErrorMessage.AddMessage($"{techString} is not charged so it cannot be recycled.");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        string batteryString = Language.main.GetOrFallback(batteryTechType.AsString(), batteryTechType.AsString());
                        ErrorMessage.AddMessage($"{batteryString} is not the default battery for {techString}.");
                        return false;
                    }
                }
                else
                {
                    ErrorMessage.AddMessage($"{techString} has no battery.");
                    return false;
                }

            }

            IBattery b2 = pickupable.GetComponent<IBattery>();
            if (b2 != null)
            {
                if (b2.charge > (b2.capacity * 0.99))
                {
                    return true;
                }
                else
                {
                    ErrorMessage.AddMessage($"{techString} is not fully charged and cannot be recycled.");
                    return false;
                }
            }
            return true;
        }

    }
}
