using System.Collections;
using TruckOrganizer.Behaviours;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Spawns the wall terminal inside the truck, locally on every client.
    /// Until the player saves a position (default F9), the terminal mounts
    /// itself automatically onto the nearest wall around the truck screen.
    /// A saved position is stored relative to the truck root, so it is
    /// reproduced in every scene and session.
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

            Transform screen = FindScreenTransform();
            if (screen == null)
            {
                Plugin.Log.LogWarning("Terminal: no TruckScreenText found in this scene, cannot anchor.");
                yield break;
            }

            try
            {
                Vector3 position;
                Quaternion rotation;

                if (Plugin.TerminalPlacementSaved.Value)
                {
                    Transform anchor = TruckRootFrom(screen);
                    Quaternion facing = AnchorFacing(anchor) * Quaternion.Euler(0f, Plugin.TerminalRotationY.Value, 0f);
                    position = anchor.position + facing * Plugin.TerminalOffset;
                    rotation = facing;
                    Plugin.Log.LogInfo($"Terminal: using saved placement relative to '{anchor.name}'.");
                }
                else if (TryAutoWallMount(screen, out position, out rotation))
                {
                    Plugin.Log.LogInfo("Terminal: auto-mounted on the nearest wall. " +
                        $"Verschieben: auf eine Wand zielen und {Plugin.PlaceTerminalKey.Value} drücken.");
                }
                else
                {
                    // No wall found; place next to the screen as last resort.
                    Quaternion facing = AnchorFacing(screen);
                    position = screen.position + facing * new Vector3(1.2f, 0f, 0f);
                    rotation = facing;
                    Plugin.Log.LogWarning("Terminal: no wall hit, using fallback position next to the screen.");
                }

                CreateOrMove(position, rotation);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Terminal creation failed: {e}");
                yield break;
            }

            Plugin.Log.LogInfo($"Terminal spawned at {_currentTerminal.transform.position} " +
                $"(screen '{screen.name}' at {screen.position}).");
        }

        /// <summary>
        /// Places (or moves) the terminal onto the surface the player is
        /// aiming at and persists the placement relative to the truck root.
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

            // Pressing the key again on (almost) the same spot flips the
            // terminal 180° - handy when a custom model faces the other way.
            if (_currentTerminal != null &&
                Vector3.Distance(_currentTerminal.transform.position, position) < 0.4f)
            {
                rotation = _currentTerminal.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                Plugin.Log.LogInfo("Terminal flipped 180°.");
            }

            CreateOrMove(position, rotation);
            PersistPlacement(position, rotation);
            Plugin.Log.LogInfo($"Terminal placed at {position} (aim placement).");
        }

        // ------------------------------------------------------------------
        // Placement helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Casts rays from a point at chest height near the truck screen and
        /// mounts the terminal on the first wall hit.
        /// </summary>
        private static bool TryAutoWallMount(Transform screen, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;

            Vector3 origin = screen.position;

            // Find the truck floor below the screen for a sane mounting height.
            float floorY = origin.y - 1.2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit floorHit, 5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                floorY = floorHit.point.y;
            }

            var eye = new Vector3(origin.x, floorY + 1.35f, origin.z);
            Vector3 forward = AnchorFacing(screen) * Vector3.forward;

            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * forward;
                if (!Physics.Raycast(eye, dir, out RaycastHit hit, 4f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if (Mathf.Abs(hit.normal.y) > 0.4f) continue; // not a wall
                if (hit.distance < 0.4f) continue;            // inside geometry

                Vector3 normal = hit.normal;
                normal.y = 0f;
                normal.Normalize();

                position = hit.point + normal * 0.08f;
                rotation = Quaternion.LookRotation(-normal, Vector3.up);
                return true;
            }

            return false;
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
        /// Stores the placement relative to the truck root in the config,
        /// inverting the math used by SpawnWhenReady.
        /// </summary>
        private static void PersistPlacement(Vector3 position, Quaternion rotation)
        {
            Transform screen = FindScreenTransform();
            if (screen == null)
            {
                Plugin.Log.LogInfo("Terminal placement: no anchor in scene; placement not persisted.");
                return;
            }
            Transform anchor = TruckRootFrom(screen);

            Quaternion anchorFacing = AnchorFacing(anchor);
            float rotY = Mathf.DeltaAngle(anchorFacing.eulerAngles.y, rotation.eulerAngles.y);
            Vector3 offset = Quaternion.Inverse(rotation) * (position - anchor.position);

            Plugin.TerminalRotationY.Value = rotY;
            Plugin.TerminalOffsetX.Value = offset.x;
            Plugin.TerminalOffsetY.Value = offset.y;
            Plugin.TerminalOffsetZ.Value = offset.z;
            Plugin.TerminalPlacementSaved.Value = true;
            Plugin.Log.LogInfo(
                $"Terminal placement persisted relative to '{anchor.name}' (offset {offset}, rotY {rotY:0.0}).");
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

        private static Transform FindScreenTransform()
        {
            // Include inactive objects; the screen may be toggled off while
            // the scene is still initializing.
            TruckScreenText[] screens = Object.FindObjectsOfType<TruckScreenText>(true);
            return screens.Length > 0 ? screens[0].transform : null;
        }

        /// <summary>Highest ancestor whose name contains "truck"; the same
        /// object in every scene, so placements reproduce identically.</summary>
        private static Transform TruckRootFrom(Transform screen)
        {
            Transform best = screen;
            for (Transform p = screen; p != null; p = p.parent)
            {
                if (p.name.IndexOf("truck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    best = p;
                }
            }
            return best;
        }
    }
}
