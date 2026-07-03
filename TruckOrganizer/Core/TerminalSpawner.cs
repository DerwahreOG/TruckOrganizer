using System.Collections;
using TruckOrganizer.Behaviours;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Spawns the wall terminal inside the truck. The truck is part of fixed
    /// scenes, so the terminal is anchored relative to the truck screen and
    /// created locally on every client (no sync required).
    /// </summary>
    public static class TerminalSpawner
    {
        private static GameObject _currentTerminal;

        public static IEnumerator SpawnWhenReady()
        {
            // Wait a few frames for scene singletons, then wait for level gen.
            yield return null;
            yield return null;

            float deadline = Time.time + 30f;
            while (Time.time < deadline && !LevelIsReady())
            {
                yield return null;
            }

            if (!ShouldSpawnHere())
            {
                Plugin.Log.LogInfo("Terminal: scene is not a truck scene, skipping.");
                yield break;
            }
            if (_currentTerminal != null) yield break;

            Transform anchor = FindTruckScreen();
            if (anchor == null)
            {
                Plugin.Log.LogWarning("Terminal: no TruckScreenText found in this scene, cannot anchor.");
                yield break;
            }

            GameObject terminal = AssetFactory.CreateTerminal();
            terminal.transform.SetParent(anchor, false);
            terminal.transform.localPosition = Plugin.TerminalOffset;
            terminal.transform.localRotation = Quaternion.Euler(0f, Plugin.TerminalRotationY.Value, 0f);

            StorageContainer container = terminal.AddComponent<StorageContainer>();
            container.label = "Lager-Terminal";

            TerminalScreen screen = terminal.GetComponentInChildren<TerminalScreen>();
            if (screen != null) screen.PowerOn();

            _currentTerminal = terminal;
            Plugin.Log.LogInfo(
                $"Terminal spawned. Anchor '{anchor.name}' at {anchor.position}, terminal at {terminal.transform.position}. " +
                "Position falsch? -> Terminal.OffsetX/Y/Z in der Config anpassen.");
        }

        private static bool LevelIsReady()
        {
            try
            {
                return SemiFunc.LevelGenDone();
            }
            catch
            {
                return true;
            }
        }

        private static bool ShouldSpawnHere()
        {
            try
            {
                if (SemiFunc.MenuLevel() || SemiFunc.RunIsLobbyMenu()) return false;
                if (SemiFunc.RunIsLevel()) return Plugin.TerminalInLevels.Value;
                // Lobby / shop scenes both contain the truck.
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Transform FindTruckScreen()
        {
            // Include inactive objects; the screen may be toggled off while
            // the scene is still initializing.
            TruckScreenText[] screens = Object.FindObjectsOfType<TruckScreenText>(true);
            if (screens.Length > 0) return screens[0].transform;
            return null;
        }
    }
}
