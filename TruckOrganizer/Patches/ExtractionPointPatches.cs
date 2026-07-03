using HarmonyLib;
using TruckOrganizer.Core;

namespace TruckOrganizer.Patches
{
    /// <summary>
    /// Spawns the chest once an extraction point has been completed
    /// ("abgegeben"). StateComplete runs every frame while the point stays in
    /// that state; ChestSpawner keeps track of points it already handled.
    /// </summary>
    [HarmonyPatch(typeof(ExtractionPoint))]
    internal static class ExtractionPointPatches
    {
        [HarmonyPatch(nameof(ExtractionPoint.StateComplete))]
        [HarmonyPostfix]
        private static void StateCompletePostfix(ExtractionPoint __instance)
        {
            ChestSpawner.OnExtractionCompleted(__instance);
        }
    }
}
