using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
namespace RecyclingBin.Patches
{

    [HarmonyPatch(typeof(Trashcan), nameof(Trashcan.IsAllowedToAdd))]
    internal class Trashcan_IsAllowedToAdd
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Trashcan), nameof(Trashcan.Update))]
    internal class Trashcan_Update
    {
        public static List<InventoryItem> inventoryItems;
        public static List<Pickupable> forcePickupItems;

        [HarmonyPrefix]
        public static bool Prefix(Trashcan __instance)
        {
            if (__instance.biohazard)
            {
                return true;
            }

            __instance.storageContainer.hoverText = "Recycling Bin";
            __instance.storageContainer.storageLabel = "Recycling Bin";
            __instance.storageContainer.container._label = "Recycling Bin";

            inventoryItems = new List<InventoryItem>();
            forcePickupItems = new List<Pickupable>();

            foreach (Trashcan.Waste waste in __instance.wasteList)
            {
                InventoryItem item = waste.inventoryItem;

                if (item is null)
                {
                    continue;
                }

                TechType techType = item.item.GetTechType();

                ITechData techData = CraftData.Get(techType);

                bool inputcheck = GameInput.GetButtonHeld(GameInput.Button.Deconstruct);

                if (!inputcheck && techType != TechType.Titanium && Main.BatteryCheck(item.item) && techData != null)
                {
                    if (CheckRequirements(__instance, item.item, techData))
                    {
                        for (int i = 0; i < techData.ingredientCount; i++)
                        {
                            IIngredient ingredient = techData.GetIngredient(i);

                            for (int j = 0; j < ingredient.amount; j++)
                            {
                                GameObject gameObject = CraftData.InstantiateFromPrefab(ingredient.techType, false);
                                if (gameObject.GetComponent<LiveMixin>() != null)
                                {
                                    GameObject.Destroy(gameObject);
                                    break;
                                }

                                gameObject.SetActive(true);
                                Pickupable pickupable = gameObject.GetComponent<Pickupable>();
                                pickupable.Pickup(false);
                                forcePickupItems.Add(pickupable);
                            }
                        }
                        break;
                    }
                }
                else
                {
                    if (inputcheck)
                    {
                        inventoryItems.Add(item);
                    }
                    else
                    {
                        forcePickupItems.Add(item.item);
                    }

                    break;
                }
            }
            forcePickupItems.ForEach((rejectedItem) => Inventory.main.ForcePickup(rejectedItem));
            inventoryItems.ForEach((item) => UnityEngine.Object.Destroy(item.item.gameObject));

            return false;
        }


        private static bool CheckRequirements(Trashcan __instance, Pickupable item, ITechData techData)
        {
            bool check = true;
            int craftCountNeeded = techData.craftAmount;
            IList<InventoryItem> inventoryItems = __instance.storageContainer.container.GetItems(item.GetTechType());
            if (inventoryItems != null && inventoryItems.Count >= craftCountNeeded)
            {
                while (craftCountNeeded > 0)
                {
                    Trashcan_Update.inventoryItems.Add(inventoryItems[craftCountNeeded - 1]);
                    craftCountNeeded--;
                }

                List<TechType> linkedItems = new List<TechType>();
                for (int i = 0; i < techData.linkedItemCount; i++)
                {
                    linkedItems.Add(techData.GetLinkedItem(i));
                }

                foreach (TechType techType in linkedItems)
                {
                    int linkedCountNeeded = linkedItems.FindAll((TechType tt) => tt == techType).Count;
                    IList<InventoryItem> inventoryItems2 = __instance.storageContainer.container.GetItems(techType);
                    IList<InventoryItem> inventoryItems3 = Inventory.main.container.GetItems(techType);
                    int count = (inventoryItems2?.Count ?? 0) + (inventoryItems3?.Count ?? 0);
                    if (count < linkedCountNeeded)
                    {
                        ErrorMessage.AddMessage($"Missing {linkedCountNeeded - (inventoryItems2?.Count + inventoryItems3?.Count)} {techType}");
                        Inventory.main.ForcePickup(item);
                        Trashcan_Update.inventoryItems.Clear();
                        return false;
                    }

                    int count1 = inventoryItems2?.Count ?? 0;
                    int count2 = inventoryItems3?.Count ?? 0;
                    while (linkedCountNeeded > 0)
                    {
                        if (count1 > 0)
                        {
                            Trashcan_Update.inventoryItems.Add(inventoryItems2[count1 - 1]);
                            count1--;
                        }
                        else if (count2 > 0)
                        {
                            Trashcan_Update.inventoryItems.Add(inventoryItems3[count2 - 1]);
                            count2--;
                        }
                        linkedCountNeeded--;
                    }
                }
            }
            else
            {
                check = false;
            }

            return check;
        }
    }
}
