using System;
using HarmonyLib;
using TruckOrganizer.Core;

namespace TruckOrganizer.Patches
{
    /// <summary>
    /// Spawns the chest once an extraction point has been completed
    /// ("abgegeben"). StateComplete runs every frame while the point stays in
    /// that state; ChestSpawner keeps track of points it already handled.
    /// The postfix must never throw, otherwise the vanilla state machine of
    /// the extraction point breaks.
    /// </summary>
    [HarmonyPatch(typeof(ExtractionPoint))]
    internal static class ExtractionPointPatches
    {
        [HarmonyPatch(nameof(ExtractionPoint.StateComplete))]
        [HarmonyPostfix]
        private static void StateCompletePostfix(ExtractionPoint __instance)
        {
            try
            {
                ChestSpawner.OnExtractionCompleted(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Chest spawn hook failed: {e}");
            }
        }
    }
}
