using System;
using HarmonyLib;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Patches
{
    // Persistence hooks and the "shop purchases go into storage" logic.
    // One class per hook so a single missing game method cannot disable the
    // remaining hooks. Every body is wrapped in try/catch: an exception
    // escaping a postfix would abort the patched vanilla method mid-flight.

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SaveFileSave))]
    internal static class SaveFileSavePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
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
    }

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.LoadGame))]
    internal static class LoadGamePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                StorageService.LoadFromDisk();
                PurchaseInterception.MigrateExistingPurchases();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"LoadGame hook failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.SaveFileCreate))]
    internal static class SaveFileCreatePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
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
    }

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.ItemPurchase))]
    internal static class ItemPurchasePatch
    {
        [HarmonyPostfix]
        private static void Postfix(StatsManager __instance, string itemName)
        {
            try
            {
                PurchaseInterception.OnItemPurchase(__instance, itemName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to move purchase into storage: {e}");
            }
        }
    }

    /// <summary>Shared logic for the purchase hooks above.</summary>
    internal static class PurchaseInterception
    {
        /// <summary>
        /// Host only: whenever an item is bought in the shop, move it from the
        /// vanilla "spawn it in the truck" bookkeeping into the shared storage.
        /// Starter grants (e.g. the starting cart) and physical infrastructure
        /// (carts, vehicles, power crystals) stay vanilla.
        /// </summary>
        public static void OnItemPurchase(StatsManager stats, string itemName)
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
                Plugin.Log.LogInfo($"Purchase '{itemName}' is excluded (cart/vehicle/crystal); leaving it vanilla.");
                return;
            }

            if (stats.itemsPurchased == null ||
                !stats.itemsPurchased.TryGetValue(itemName, out int count) || count <= 0)
            {
                return;
            }

            SetPurchasedCount(stats, itemName, count - 1);
            StorageService.HostAdd(itemName);
            Plugin.Log.LogInfo($"Moved purchase '{itemName}' into storage.");
        }

        /// <summary>
        /// When an existing save is loaded: move previously purchased items
        /// into the storage, and move excluded item types (carts, crystals)
        /// that ended up in the storage in earlier mod versions back out.
        /// </summary>
        public static void MigrateExistingPurchases()
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

            // Reverse migration for items that no longer belong in storage.
            foreach (var entry in new System.Collections.Generic.List<
                         System.Collections.Generic.KeyValuePair<string, int>>(StorageService.ContentsSnapshot()))
            {
                if (!IsExcludedFromStorage(entry.Key) || entry.Value <= 0) continue;

                stats.itemsPurchased.TryGetValue(entry.Key, out int current);
                SetPurchasedCount(stats, entry.Key, current + entry.Value);
                StorageService.HostAdd(entry.Key, -entry.Value);
                Plugin.Log.LogInfo($"Moved {entry.Value}x '{entry.Key}' back out of storage (excluded type).");
            }
        }

        /// <summary>
        /// Carts, vehicles and power crystals are physical infrastructure the
        /// game expects to exist in the world; keep them out of the storage.
        /// </summary>
        public static bool IsExcludedFromStorage(string itemName)
        {
            Item item = StorageService.ResolveItem(itemName);
            if (item == null) return false;
            return item.itemType == SemiFunc.itemType.cart
                || item.itemType == SemiFunc.itemType.pocket_cart
                || item.itemType == SemiFunc.itemType.vehicle
                || item.itemType == SemiFunc.itemType.power_crystal;
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
