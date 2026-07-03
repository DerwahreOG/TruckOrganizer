using System;
using System.Collections.Generic;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Maps upgrade item asset names to the matching PunManager upgrade call.
    /// PunManager applies the stat change and handles multiplayer sync itself.
    /// </summary>
    public static class UpgradeMap
    {
        private static readonly (string keyword, Func<string, int> apply)[] Mappings =
        {
            ("Health", id => PunManager.instance.UpgradePlayerHealth(id)),
            ("Stamina", id => PunManager.instance.UpgradePlayerEnergy(id)),
            ("Energy", id => PunManager.instance.UpgradePlayerEnergy(id)),
            ("Extra Jump", id => PunManager.instance.UpgradePlayerExtraJump(id)),
            ("Grab Range", id => PunManager.instance.UpgradePlayerGrabRange(id)),
            ("Range", id => PunManager.instance.UpgradePlayerGrabRange(id)),
            ("Grab Strength", id => PunManager.instance.UpgradePlayerGrabStrength(id)),
            ("Strength", id => PunManager.instance.UpgradePlayerGrabStrength(id)),
            ("Grab Throw", id => PunManager.instance.UpgradePlayerThrowStrength(id)),
            ("Throw", id => PunManager.instance.UpgradePlayerThrowStrength(id)),
            ("Sprint Speed", id => PunManager.instance.UpgradePlayerSprintSpeed(id)),
            ("Speed", id => PunManager.instance.UpgradePlayerSprintSpeed(id)),
            ("Tumble Launch", id => PunManager.instance.UpgradePlayerTumbleLaunch(id)),
            ("Tumble Climb", id => PunManager.instance.UpgradePlayerTumbleClimb(id)),
            ("Tumble Wings", id => PunManager.instance.UpgradePlayerTumbleWings(id)),
            ("Crouch Rest", id => PunManager.instance.UpgradePlayerCrouchRest(id)),
            ("Map Player Count", id => PunManager.instance.UpgradeMapPlayerCount(id)),
            ("Death Head", id => PunManager.instance.UpgradeDeathHeadBattery(id)),
        };

        /// <summary>Host only. Applies the upgrade for the given player.</summary>
        public static bool TryApply(string itemName, string steamId)
        {
            if (PunManager.instance == null || string.IsNullOrEmpty(steamId)) return false;
            if (!StorageService.IsPlayerUpgrade(itemName)) return false;

            foreach (var (keyword, apply) in Mappings)
            {
                if (itemName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;
                try
                {
                    apply(steamId);
                    TrackUpgradePurchase(itemName);
                    Plugin.Log.LogInfo($"Applied upgrade '{itemName}' to {steamId}.");
                    return true;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Applying upgrade '{itemName}' failed: {e}");
                    return false;
                }
            }
            return false;
        }

        private static void TrackUpgradePurchase(string itemName)
        {
            try
            {
                StatsManager.instance?.AddItemsUpgradesPurchased(itemName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not track upgrade usage: {e.Message}");
            }
        }
    }
}
