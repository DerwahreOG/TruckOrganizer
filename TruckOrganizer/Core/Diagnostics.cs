using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Debug helper bound to a configurable key: dumps the mod state into the
    /// BepInEx log and spawns a chest right in front of the player so the
    /// storage can be reached even when the automatic spawns fail.
    /// </summary>
    public static class Diagnostics
    {
        public static void DumpAndSpawnDebugChest()
        {
            Plugin.Log.LogInfo(BuildReport());

            Camera cam = Camera.main;
            if (cam == null)
            {
                Plugin.Log.LogWarning("Debug chest: no main camera found.");
                return;
            }

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;

            Vector3 position = cam.transform.position + forward * 1.8f;
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
            }
            else
            {
                position.y = cam.transform.position.y - 1.4f;
            }

            ChestSpawner.SpawnLocalChest(position, Quaternion.LookRotation(-forward, Vector3.up));
            Plugin.Log.LogInfo("Debug chest spawned in front of the player.");
        }

        private static string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== TruckOrganizer diagnostics =====");
            sb.AppendLine($"Version: {Plugin.Version}, PatchesApplied: {Plugin.PatchesApplied}");
            sb.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");

            AppendSafe(sb, "IsHost", () => SafeGame.IsHost().ToString());
            AppendSafe(sb, "IsMultiplayer", () => SafeGame.IsMultiplayer().ToString());
            AppendSafe(sb, "RunIsLevel", () => SafeGame.RunIsLevel().ToString());
            AppendSafe(sb, "RunIsShop", () => SafeGame.RunIsShop().ToString());
            AppendSafe(sb, "RunIsLobby", () => SemiFunc.RunIsLobby().ToString());
            AppendSafe(sb, "MenuLevel", () => SemiFunc.MenuLevel().ToString());
            AppendSafe(sb, "LevelGenDone", () => SemiFunc.LevelGenDone().ToString());
            AppendSafe(sb, "TruckScreens", () =>
                UnityEngine.Object.FindObjectsOfType<TruckScreenText>(true).Length.ToString());
            AppendSafe(sb, "ExtractionPoints", () =>
                UnityEngine.Object.FindObjectsOfType<ExtractionPoint>(true).Length.ToString());
            AppendSafe(sb, "StatsManager", () => (StatsManager.instance != null).ToString());
            AppendSafe(sb, "Storage", () =>
            {
                string s = StorageService.Serialize();
                return string.IsNullOrEmpty(s) ? "(leer)" : s;
            });

            sb.Append("======================================");
            return sb.ToString();
        }

        private static void AppendSafe(StringBuilder sb, string label, Func<string> value)
        {
            try
            {
                sb.AppendLine($"{label}: {value()}");
            }
            catch (Exception e)
            {
                sb.AppendLine($"{label}: FEHLER ({e.GetType().Name}: {e.Message})");
            }
        }
    }
}
