using System;
using HarmonyLib;
using TruckOrganizer.Core;

namespace TruckOrganizer.Patches
{
    /// <summary>
    /// Spawns the chest once an extraction point has been completed
    /// ("abgegeben"). Several hooks cover the different paths the game takes
    /// into the Complete state; ChestSpawner deduplicates per point.
    /// No hook may throw, otherwise the vanilla state machine breaks.
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
                Plugin.Log.LogError($"Chest spawn hook (StateComplete) failed: {e}");
            }
        }

        [HarmonyPatch(nameof(ExtractionPoint.StateSet))]
        [HarmonyPostfix]
        private static void StateSetPostfix(ExtractionPoint __instance, ExtractionPoint.State newState)
        {
            try
            {
                if (newState == ExtractionPoint.State.Complete)
                {
                    ChestSpawner.OnExtractionCompleted(__instance);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Chest spawn hook (StateSet) failed: {e}");
            }
        }

        [HarmonyPatch(nameof(ExtractionPoint.StateSetRPC))]
        [HarmonyPostfix]
        private static void StateSetRPCPostfix(ExtractionPoint __instance, ExtractionPoint.State state)
        {
            try
            {
                if (state == ExtractionPoint.State.Complete)
                {
                    ChestSpawner.OnExtractionCompleted(__instance);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Chest spawn hook (StateSetRPC) failed: {e}");
            }
        }
    }

    /// <summary>
    /// Extra safety net: RoundDirector.ExtractionCompleted fires once per
    /// completed extraction point on the host.
    /// </summary>
    [HarmonyPatch(typeof(RoundDirector))]
    internal static class RoundDirectorPatches
    {
        [HarmonyPatch(nameof(RoundDirector.ExtractionCompleted))]
        [HarmonyPostfix]
        private static void ExtractionCompletedPostfix(RoundDirector __instance)
        {
            try
            {
                ChestSpawner.OnExtractionCompleted(__instance.extractionPointCurrent);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Chest spawn hook (RoundDirector) failed: {e}");
            }
        }
    }
}
