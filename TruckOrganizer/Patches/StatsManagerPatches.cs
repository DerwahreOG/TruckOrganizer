using System;
using HarmonyLib;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Patches
{
    /// <summary>
    /// Persistence hooks and the "shop purchases go into storage" logic.
    /// The storage file is written next to the BepInEx config, keyed by the
    /// host's current save file, so content survives between sessions.
    /// </summary>
    [HarmonyPatch(typeof(StatsManager))]
    internal static class StatsManagerPatches
    {
        [HarmonyPatch(nameof(StatsManager.SaveFileSave))]
        [HarmonyPostfix]
        private static void SaveFileSavePostfix()
        {
            StorageService.SaveToDisk();
        }

        [HarmonyPatch(nameof(StatsManager.LoadGame))]
        [HarmonyPostfix]
        private static void LoadGamePostfix()
        {
            StorageService.LoadFromDisk();
            MigrateExistingPurchases();
        }

        [HarmonyPatch(nameof(StatsManager.SaveFileCreate))]
        [HarmonyPostfix]
        private static void SaveFileCreatePostfix()
        {
            StorageService.ResetForNewSave();
        }

        /// <summary>
        /// Host only: whenever an item is bought in the shop, move it from the
        /// vanilla "spawn it in the truck" bookkeeping into the shared storage.
        /// </summary>
        [HarmonyPatch(nameof(StatsManager.ItemPurchase))]
        [HarmonyPostfix]
        private static void ItemPurchasePostfix(StatsManager __instance, string itemName)
        {
            if (!Plugin.PurchasesGoToStorage.Value) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            try
            {
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
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            StatsManager stats = StatsManager.instance;
            if (stats == null || stats.itemsPurchased == null) return;

            try
            {
                foreach (string itemName in new System.Collections.Generic.List<string>(stats.itemsPurchased.Keys))
                {
                    int count = stats.itemsPurchased[itemName];
                    if (count <= 0) continue;

                    SetPurchasedCount(stats, itemName, 0);
                    StorageService.HostAdd(itemName, count);
                    Plugin.Log.LogInfo($"Migrated {count}x '{itemName}' into storage.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to migrate purchases into storage: {e}");
            }
        }

        private static void SetPurchasedCount(StatsManager stats, string itemName, int value)
        {
            value = Mathf.Max(0, value);
            stats.itemsPurchased[itemName] = value;

            // Keep clients in sync with the host's bookkeeping if possible.
            try
            {
                if (SemiFunc.IsMasterClient() && PunManager.instance != null)
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
