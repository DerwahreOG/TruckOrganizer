using System;
using HarmonyLib;
using TruckOrganizer.Core;

namespace TruckOrganizer.Patches
{
    // Each hook lives in its own class: patching is done per class, so if the
    // game renames one method after an update, only that single hook is lost
    // and everything else keeps working. ChestSpawner deduplicates per point.

    [HarmonyPatch(typeof(ExtractionPoint), nameof(ExtractionPoint.StateComplete))]
    internal static class ExtractionStateCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ExtractionPoint __instance)
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
    }

    [HarmonyPatch(typeof(ExtractionPoint), nameof(ExtractionPoint.StateSet))]
    internal static class ExtractionStateSetPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ExtractionPoint __instance, ExtractionPoint.State newState)
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
    }

    [HarmonyPatch(typeof(ExtractionPoint), nameof(ExtractionPoint.StateSetRPC))]
    internal static class ExtractionStateSetRPCPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ExtractionPoint __instance, ExtractionPoint.State state)
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

    [HarmonyPatch(typeof(RoundDirector), nameof(RoundDirector.ExtractionCompleted))]
    internal static class RoundDirectorExtractionCompletedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RoundDirector __instance)
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
