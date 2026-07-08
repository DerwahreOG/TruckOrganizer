using System.Collections.Generic;
using TruckOrganizer.Behaviours;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Decides where the chest may stand (host) and creates the local chest
    /// object on every client. The environment around extraction points is
    /// procedural, so a valid free spot is searched at runtime.
    /// </summary>
    public static class ChestSpawner
    {
        private static readonly HashSet<ExtractionPoint> _handledPoints = new HashSet<ExtractionPoint>();

        // Approximate half extents of the chest for clearance checks.
        private static readonly Vector3 ChestHalfExtents = new Vector3(0.5f, 0.35f, 0.35f);

        public static void OnSceneLoaded()
        {
            _handledPoints.Clear();
        }

        /// <summary>
        /// Called (host only) when an extraction point reaches the Complete
        /// state. Finds a free spot and tells everyone to spawn the chest.
        /// </summary>
        public static void OnExtractionCompleted(ExtractionPoint point)
        {
            if (!Plugin.ChestEnabled.Value) return;
            if (!SafeGame.IsHost()) return;
            if (point == null || point.isShop) return;
            if (!SafeGame.RunIsLevel()) return;
            if (!_handledPoints.Add(point)) return;

            Vector3 origin = point.transform.position;
            if (!TryFindSpot(point, out Vector3 position, out Quaternion rotation))
            {
                // Last resort: right in front of the point.
                position = origin + point.transform.forward * 2f;
                rotation = Quaternion.LookRotation(-point.transform.forward, Vector3.up);
            }

            Plugin.Log.LogInfo($"Extraction point completed; spawning chest at {position}.");
            NetworkEvents.BroadcastChestSpawn(position, rotation);
        }

        /// <summary>Creates the chest locally (runs on every client).</summary>
        public static void SpawnLocalChest(Vector3 position, Quaternion rotation)
        {
            try
            {
                GameObject chest = AssetFactory.CreateChest();
                chest.transform.SetPositionAndRotation(position, rotation);

                StorageContainer container = chest.AddComponent<StorageContainer>();
                container.label = "Vorratstruhe";
                Plugin.Log.LogInfo($"Chest created at {position}.");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Chest creation failed: {e}");
            }
        }

        // ------------------------------------------------------------------
        // Placement search
        // ------------------------------------------------------------------

        private static bool TryFindSpot(ExtractionPoint point, out Vector3 position, out Quaternion rotation)
        {
            Vector3 origin = point.transform.position;
            Vector3 eye = origin + Vector3.up * 1.4f;

            float bestScore = float.MaxValue;
            position = default;
            rotation = default;
            bool found = false;

            float[] radii = { 1.8f, 2.4f, 3.2f, 4.0f, 5.0f };
            const int angleSteps = 16;

            for (int r = 0; r < radii.Length; r++)
            {
                for (int a = 0; a < angleSteps; a++)
                {
                    float angle = a * (360f / angleSteps);
                    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Vector3 candidateTop = origin + dir * radii[r] + Vector3.up * 1.5f;

                    if (!TryProjectToGround(candidateTop, out Vector3 ground)) continue;

                    Vector3 center = ground + Vector3.up * (ChestHalfExtents.y + 0.05f);
                    Quaternion facing = Quaternion.LookRotation((origin - ground).normalized.WithY(0f), Vector3.up);

                    if (Physics.CheckBox(center, ChestHalfExtents, facing, DefaultMask(), QueryTriggerInteraction.Ignore))
                    {
                        continue; // spot is blocked by geometry or props
                    }

                    // Prefer spots that are visible from the extraction point so
                    // the chest is not hidden behind walls.
                    bool visible = !Physics.Linecast(eye, center, DefaultMask(), QueryTriggerInteraction.Ignore);
                    float score = radii[r] + (visible ? 0f : 100f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        position = ground;
                        rotation = facing;
                        found = true;
                    }
                }

                // Take the closest fully valid ring; only keep searching if all
                // spots so far were obstructed or invisible.
                if (found && bestScore < 100f) return true;
            }

            return found;
        }

        private static bool TryProjectToGround(Vector3 from, out Vector3 ground)
        {
            ground = default;
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 4f, DefaultMask(), QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (Vector3.Angle(hit.normal, Vector3.up) > 25f) return false;
            ground = hit.point;
            return true;
        }

        private static int DefaultMask()
        {
            // Everything except the physics-grab layer so loose valuables do
            // not block otherwise fine spots.
            int mask = Physics.DefaultRaycastLayers;
            try
            {
                mask &= ~SemiFunc.LayerMaskGetPhysGrabObject().value;
            }
            catch { /* keep default mask */ }
            return mask;
        }

        private static Vector3 WithY(this Vector3 v, float y)
        {
            v.y = y;
            if (v.sqrMagnitude < 0.001f) v = Vector3.forward;
            return v;
        }
    }
}
