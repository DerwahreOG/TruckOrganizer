using System.Collections;
using TruckOrganizer.Behaviours;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Spawns the wall terminal inside the truck. The terminal is anchored
    /// relative to the truck screen and created locally on every client.
    /// Players can re-place it at any wall they aim at (default F9); the new
    /// position is stored in the config relative to the anchor, so it is
    /// restored in every scene and session.
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

            try
            {
                Quaternion facing = AnchorFacing(anchor) * Quaternion.Euler(0f, Plugin.TerminalRotationY.Value, 0f);
                Vector3 position = anchor.position + facing * Plugin.TerminalOffset;
                CreateOrMove(position, facing);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Terminal creation failed: {e}");
                yield break;
            }

            Plugin.Log.LogInfo(
                $"Terminal spawned. Anchor '{anchor.name}' at {anchor.position}, " +
                $"terminal at {_currentTerminal.transform.position}. " +
                $"Position falsch? -> Im Spiel auf eine Wand zielen und {Plugin.PlaceTerminalKey.Value} drücken.");
        }

        /// <summary>
        /// Places (or moves) the terminal onto the surface the player is
        /// aiming at and persists the placement relative to the truck screen.
        /// </summary>
        public static void PlaceAtAim()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Plugin.Log.LogWarning("Terminal placement: no camera.");
                return;
            }

            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 4.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Plugin.Log.LogWarning("Terminal placement: nothing hit; aim at a wall within 4m.");
                return;
            }

            Vector3 normal = hit.normal;
            if (Mathf.Abs(normal.y) > 0.6f)
            {
                Plugin.Log.LogWarning("Terminal placement: aim at a WALL (not floor/ceiling).");
                return;
            }
            normal.y = 0f;
            normal.Normalize();

            // Terminal front is -Z; +Z (its back) must point into the wall.
            Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);
            Vector3 position = hit.point + normal * 0.08f;

            CreateOrMove(position, rotation);
            PersistPlacement(position, rotation);
            Plugin.Log.LogInfo($"Terminal placed at {position} (aim placement).");
        }

        private static void CreateOrMove(Vector3 position, Quaternion rotation)
        {
            if (_currentTerminal == null)
            {
                GameObject terminal = AssetFactory.CreateTerminal();

                StorageContainer container = terminal.AddComponent<StorageContainer>();
                container.label = "Lager-Terminal";

                _currentTerminal = terminal;
                terminal.transform.SetPositionAndRotation(position, rotation);

                TerminalScreen screen = terminal.GetComponentInChildren<TerminalScreen>();
                if (screen != null) screen.PowerOn();
            }
            else
            {
                _currentTerminal.transform.SetPositionAndRotation(position, rotation);
            }
        }

        /// <summary>
        /// Stores the placement relative to the truck screen anchor in the
        /// config, inverting the math used by SpawnWhenReady.
        /// </summary>
        private static void PersistPlacement(Vector3 position, Quaternion rotation)
        {
            Transform anchor = FindTruckScreen();
            if (anchor == null)
            {
                Plugin.Log.LogInfo("Terminal placement: no anchor in scene; placement not persisted.");
                return;
            }

            Quaternion anchorFacing = AnchorFacing(anchor);
            float rotY = Mathf.DeltaAngle(anchorFacing.eulerAngles.y, rotation.eulerAngles.y);
            Vector3 offset = Quaternion.Inverse(rotation) * (position - anchor.position);

            Plugin.TerminalRotationY.Value = rotY;
            Plugin.TerminalOffsetX.Value = offset.x;
            Plugin.TerminalOffsetY.Value = offset.y;
            Plugin.TerminalOffsetZ.Value = offset.z;
            Plugin.Log.LogInfo($"Terminal placement persisted (offset {offset}, rotY {rotY:0.0}).");
        }

        private static Quaternion AnchorFacing(Transform anchor)
        {
            Vector3 f = anchor.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            return Quaternion.LookRotation(f.normalized, Vector3.up);
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
