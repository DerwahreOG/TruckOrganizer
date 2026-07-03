using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Spawns physical items for a player (host only) and tries to put them
    /// straight into a free inventory slot on the requesting client.
    /// </summary>
    public static class ItemSpawner
    {
        /// <summary>Host only. Returns the spawned object or null.</summary>
        public static GameObject SpawnForPlayer(Item item, string steamId)
        {
            if (item == null || item.prefab == null) return null;

            PlayerAvatar target = FindAvatar(steamId);
            Vector3 position;
            Quaternion rotation = Quaternion.identity;
            if (target != null)
            {
                Transform t = target.transform;
                position = t.position + t.forward * 1.0f + Vector3.up * 1.2f;
                rotation = Quaternion.LookRotation(-t.forward, Vector3.up);
            }
            else
            {
                position = Vector3.up;
            }

            GameObject spawned;
            if (SemiFunc.IsMultiplayer())
            {
                spawned = PhotonNetwork.InstantiateRoomObject(item.prefab.ResourcePath, position, rotation);
            }
            else
            {
                spawned = Object.Instantiate(item.prefab.Prefab, position, rotation);
            }

            if (spawned == null) return null;

            PhotonView view = spawned.GetComponent<PhotonView>();
            int viewId = view != null ? view.ViewID : -1;
            NetworkEvents.BroadcastEquipHint(steamId, viewId);
            return spawned;
        }

        /// <summary>
        /// Runs on every client; only the addressed player reacts and tries to
        /// equip the freshly spawned item into a free inventory slot.
        /// </summary>
        public static void HandleEquipHint(string steamId, int photonViewId)
        {
            if (photonViewId < 0) return;
            string localId = StorageService.LocalSteamId();
            if (string.IsNullOrEmpty(localId) || localId != steamId) return;
            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(TryEquipRoutine(photonViewId));
            }
        }

        private static IEnumerator TryEquipRoutine(int photonViewId)
        {
            float deadline = Time.time + 3f;
            ItemEquippable equippable = null;

            while (Time.time < deadline)
            {
                PhotonView view = PhotonView.Find(photonViewId);
                if (view != null)
                {
                    equippable = view.GetComponent<ItemEquippable>();
                    if (equippable != null) break;
                }
                yield return null;
            }

            if (equippable == null) yield break;

            int spot = -1;
            try
            {
                if (Inventory.instance != null)
                {
                    spot = Inventory.instance.GetFirstFreeInventorySpotIndex();
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Could not query inventory spots: {e.Message}");
            }

            if (spot < 0)
            {
                Plugin.Log.LogInfo("No free inventory slot; item stays in the world.");
                yield break;
            }

            try
            {
                equippable.RequestEquip(spot);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Auto-equip failed, item stays in the world: {e.Message}");
            }
        }

        private static PlayerAvatar FindAvatar(string steamId)
        {
            if (string.IsNullOrEmpty(steamId)) return null;
            try
            {
                PlayerAvatar avatar = SemiFunc.PlayerAvatarGetFromSteamID(steamId);
                if (avatar != null) return avatar;
            }
            catch { /* fall through */ }
            try
            {
                return SemiFunc.PlayerAvatarLocal();
            }
            catch
            {
                return null;
            }
        }
    }
}
