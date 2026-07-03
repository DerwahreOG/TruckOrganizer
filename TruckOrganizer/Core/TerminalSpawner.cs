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

            if (!ShouldSpawnHere()) yield break;
            if (_currentTerminal != null) yield break;

            Transform anchor = FindTruckScreen();
            if (anchor == null)
            {
                Plugin.Log.LogInfo("No truck screen found in this scene; terminal not spawned.");
                yield break;
            }

            Vector3 offset = Plugin.TerminalOffset.Value;
            Vector3 euler = Plugin.TerminalEulerOffset.Value;

            GameObject terminal = AssetFactory.CreateTerminal();
            terminal.transform.SetParent(anchor, false);
            terminal.transform.localPosition = offset;
            terminal.transform.localRotation = Quaternion.Euler(euler);

            StorageContainer container = terminal.AddComponent<StorageContainer>();
            container.label = "Lager-Terminal";

            TerminalScreen screen = terminal.GetComponentInChildren<TerminalScreen>();
            if (screen != null) screen.PowerOn();

            _currentTerminal = terminal;
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
            TruckScreenText screen = Object.FindObjectOfType<TruckScreenText>();
            return screen != null ? screen.transform : null;
        }
    }
}
