using System;
using HarmonyLib;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Patches
{
    /// <summary>
    /// Persistence hooks and the "shop purchases go into storage" logic.
    /// Every postfix body is wrapped in try/catch: an exception escaping a
    /// Harmony postfix would abort the patched vanilla method mid-flight and
    /// corrupt the game's save/run state.
    /// </summary>
    [HarmonyPatch(typeof(StatsManager))]
    internal static class StatsManagerPatches
    {
        [HarmonyPatch(nameof(StatsManager.SaveFileSave))]
        [HarmonyPostfix]
        private static void SaveFileSavePostfix()
        {
            try
            {
                StorageService.SaveToDisk();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"SaveFileSave hook failed: {e}");
            }
        }

        [HarmonyPatch(nameof(StatsManager.LoadGame))]
        [HarmonyPostfix]
        private static void LoadGamePostfix()
        {
            try
            {
                StorageService.LoadFromDisk();
                MigrateExistingPurchases();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"LoadGame hook failed: {e}");
            }
        }

        [HarmonyPatch(nameof(StatsManager.SaveFileCreate))]
        [HarmonyPostfix]
        private static void SaveFileCreatePostfix()
        {
            try
            {
                StorageService.ResetForNewSave();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"SaveFileCreate hook failed: {e}");
            }
        }

        /// <summary>
        /// Host only: whenever an item is bought in the shop, move it from the
        /// vanilla "spawn it in the truck" bookkeeping into the shared storage.
        /// Only real shop purchases are intercepted; starter grants (e.g. the
        /// starting cart on a new game) and carts stay vanilla.
        /// </summary>
        [HarmonyPatch(nameof(StatsManager.ItemPurchase))]
        [HarmonyPostfix]
        private static void ItemPurchasePostfix(StatsManager __instance, string itemName)
        {
            try
            {
                if (!Plugin.PurchasesGoToStorage.Value) return;
                if (!SafeGame.IsHost()) return;

                if (!SafeGame.RunIsShop())
                {
                    Plugin.Log.LogInfo($"Purchase '{itemName}' outside the shop; leaving it vanilla.");
                    return;
                }

                if (IsExcludedFromStorage(itemName))
                {
                    Plugin.Log.LogInfo($"Purchase '{itemName}' is a cart/vehicle; leaving it vanilla.");
                    return;
                }

                if (__instance.itemsPurchased == null ||
                    !__instance.itemsPurchased.TryGetValue(itemName, out int count) || count <= 0)
                {
                    return;
                }

                SetPurchasedCount(__instance, itemName, count - 1);
                StorageService.HostAdd(itemName);
                Plugin.Log.LogInfo($"Moved purchase '{itemName}' into storage.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to move purchase into storage: {e}");
            }
        }

        /// <summary>
        /// When an existing save is loaded and the storage feature is active,
        /// move previously purchased items into the storage as well so they
        /// show up in the chest instead of cluttering the truck.
        /// </summary>
        private static void MigrateExistingPurchases()
        {
            if (!Plugin.PurchasesGoToStorage.Value) return;
            if (!SafeGame.IsHost()) return;

            StatsManager stats = StatsManager.instance;
            if (stats == null || stats.itemsPurchased == null) return;

            foreach (string itemName in new System.Collections.Generic.List<string>(stats.itemsPurchased.Keys))
            {
                int count = stats.itemsPurchased[itemName];
                if (count <= 0) continue;
                if (IsExcludedFromStorage(itemName)) continue;

                SetPurchasedCount(stats, itemName, 0);
                StorageService.HostAdd(itemName, count);
                Plugin.Log.LogInfo($"Migrated {count}x '{itemName}' into storage.");
            }
        }

        /// <summary>
        /// Carts and vehicles are physical infrastructure the game expects to
        /// exist in the truck; keep them out of the storage entirely.
        /// </summary>
        private static bool IsExcludedFromStorage(string itemName)
        {
            Item item = StorageService.ResolveItem(itemName);
            if (item == null) return false;
            return item.itemType == SemiFunc.itemType.cart
                || item.itemType == SemiFunc.itemType.pocket_cart
                || item.itemType == SemiFunc.itemType.vehicle;
        }

        private static void SetPurchasedCount(StatsManager stats, string itemName, int value)
        {
            value = Mathf.Max(0, value);
            stats.itemsPurchased[itemName] = value;

            // Keep clients in sync with the host's bookkeeping if possible.
            try
            {
                if (SafeGame.IsMultiplayer() && PunManager.instance != null)
                {
                    PunManager.instance.UpdateStat("itemsPurchased", itemName, value);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not sync purchase count for '{itemName}': {e.Message}");
            }
        }
    }
}
