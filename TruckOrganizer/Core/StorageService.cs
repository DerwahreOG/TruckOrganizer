using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Host-authoritative shared storage. The host owns the real dictionary,
    /// persists it as JSON next to the BepInEx config and pushes snapshots to
    /// all clients whenever the content changes. Clients only ever hold a
    /// read-only mirror and send take/use requests to the host.
    /// </summary>
    public static class StorageService
    {
        // item asset name (e.g. "Item Gun Handgun") -> stored amount
        private static readonly Dictionary<string, int> _storage = new Dictionary<string, int>();

        /// <summary>Raised whenever the local mirror changes (used by the UI).</summary>
        public static event Action ContentsChanged;

        public static IReadOnlyDictionary<string, int> Contents => _storage;

        private static string SaveDirectory =>
            Path.Combine(Paths.ConfigPath, "TruckOrganizer");

        // ------------------------------------------------------------------
        // Host-side state changes
        // ------------------------------------------------------------------

        public static void HostAdd(string itemName, int amount = 1)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            _storage.TryGetValue(itemName, out int current);
            _storage[itemName] = Mathf.Max(0, current + amount);
            if (_storage[itemName] == 0) _storage.Remove(itemName);
            AfterHostChange();
        }

        public static void HostHandleTakeRequest(string itemName, string requesterSteamId)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!_storage.TryGetValue(itemName, out int count) || count <= 0)
            {
                Plugin.Log.LogWarning($"Take request for '{itemName}' denied (not in storage).");
                return;
            }

            Item item = ResolveItem(itemName);
            if (item == null)
            {
                Plugin.Log.LogError($"Take request for '{itemName}' denied (unknown item).");
                return;
            }

            GameObject spawned = ItemSpawner.SpawnForPlayer(item, requesterSteamId);
            if (spawned == null)
            {
                Plugin.Log.LogError($"Failed to spawn '{itemName}'. Storage not changed.");
                return;
            }

            _storage[itemName] = count - 1;
            if (_storage[itemName] <= 0) _storage.Remove(itemName);
            AfterHostChange();
        }

        public static void HostHandleUseRequest(string itemName, string requesterSteamId)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!_storage.TryGetValue(itemName, out int count) || count <= 0)
            {
                Plugin.Log.LogWarning($"Use request for '{itemName}' denied (not in storage).");
                return;
            }

            if (!UpgradeMap.TryApply(itemName, requesterSteamId))
            {
                Plugin.Log.LogWarning($"'{itemName}' is not a usable upgrade.");
                return;
            }

            _storage[itemName] = count - 1;
            if (_storage[itemName] <= 0) _storage.Remove(itemName);
            AfterHostChange();
        }

        private static void AfterHostChange()
        {
            SaveToDisk();
            NetworkEvents.BroadcastSnapshot(Serialize());
            ContentsChanged?.Invoke();
        }

        // ------------------------------------------------------------------
        // Client mirror
        // ------------------------------------------------------------------

        public static void ApplySnapshot(string payload)
        {
            _storage.Clear();
            foreach (var pair in Deserialize(payload))
            {
                _storage[pair.Key] = pair.Value;
            }
            ContentsChanged?.Invoke();
        }

        // ------------------------------------------------------------------
        // Requests (work from host and clients alike)
        // ------------------------------------------------------------------

        public static void RequestTake(string itemName)
        {
            NetworkEvents.SendTakeRequest(itemName, LocalSteamId());
        }

        public static void RequestUse(string itemName)
        {
            NetworkEvents.SendUseRequest(itemName, LocalSteamId());
        }

        public static void RequestSnapshot()
        {
            NetworkEvents.SendSnapshotRequest();
        }

        internal static string LocalSteamId()
        {
            try
            {
                PlayerAvatar avatar = SemiFunc.PlayerAvatarLocal();
                if (avatar != null && !string.IsNullOrEmpty(avatar.steamID)) return avatar.steamID;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not resolve local steam id: {e.Message}");
            }
            return string.Empty;
        }

        // ------------------------------------------------------------------
        // Item helpers
        // ------------------------------------------------------------------

        public static Item ResolveItem(string itemName)
        {
            StatsManager stats = StatsManager.instance;
            if (stats == null || stats.itemDictionary == null) return null;
            stats.itemDictionary.TryGetValue(itemName, out Item item);
            return item;
        }

        public static bool IsPlayerUpgrade(string itemName)
        {
            Item item = ResolveItem(itemName);
            return item != null && item.itemType == SemiFunc.itemType.player_upgrade;
        }

        public static string DisplayName(string itemName)
        {
            Item item = ResolveItem(itemName);
            if (item != null && !string.IsNullOrEmpty(item.itemName)) return item.itemName;
            return itemName;
        }

        // ------------------------------------------------------------------
        // Persistence (host only). One JSON-ish file per save game.
        // ------------------------------------------------------------------

        private static string CurrentSaveKey()
        {
            try
            {
                string file = StatsManager.instance != null ? StatsManager.instance.saveFileCurrent : null;
                if (!string.IsNullOrEmpty(file)) return SanitizeFileName(file);
            }
            catch { /* stripped assembly members can behave unexpectedly */ }
            return "default";
        }

        private static string SanitizeFileName(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }

        public static void SaveToDisk()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            try
            {
                Directory.CreateDirectory(SaveDirectory);
                string path = Path.Combine(SaveDirectory, CurrentSaveKey() + ".txt");
                File.WriteAllText(path, Serialize());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to persist storage: {e}");
            }
        }

        public static void LoadFromDisk()
        {
            try
            {
                string path = Path.Combine(SaveDirectory, CurrentSaveKey() + ".txt");
                _storage.Clear();
                if (File.Exists(path))
                {
                    foreach (var pair in Deserialize(File.ReadAllText(path)))
                    {
                        _storage[pair.Key] = pair.Value;
                    }
                }
                ContentsChanged?.Invoke();
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    NetworkEvents.BroadcastSnapshot(Serialize());
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to load storage: {e}");
            }
        }

        public static void ResetForNewSave()
        {
            _storage.Clear();
            ContentsChanged?.Invoke();
        }

        // ------------------------------------------------------------------
        // Wire format: "name=count;name=count"
        // ------------------------------------------------------------------

        public static string Serialize()
        {
            return string.Join(";", _storage
                .Where(p => p.Value > 0)
                .Select(p => p.Key + "=" + p.Value));
        }

        private static IEnumerable<KeyValuePair<string, int>> Deserialize(string payload)
        {
            if (string.IsNullOrEmpty(payload)) yield break;
            foreach (string entry in payload.Split(';'))
            {
                int idx = entry.LastIndexOf('=');
                if (idx <= 0) continue;
                string name = entry.Substring(0, idx);
                if (int.TryParse(entry.Substring(idx + 1), out int count) && count > 0)
                {
                    yield return new KeyValuePair<string, int>(name, count);
                }
            }
        }
    }
}
