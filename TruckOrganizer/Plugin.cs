using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TruckOrganizer.Behaviours;
using TruckOrganizer.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TruckOrganizer
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "truckorganizer.storage";
        public const string Name = "TruckOrganizer";
        public const string Version = "0.1.6";

        public static Plugin Instance { get; private set; }
        public static bool PatchesApplied { get; private set; }
        internal static ManualLogSource Log;

        // --- Config ---
        public static ConfigEntry<bool> ChestEnabled;
        public static ConfigEntry<bool> TerminalEnabled;
        public static ConfigEntry<bool> TerminalInLevels;
        public static ConfigEntry<bool> PurchasesGoToStorage;
        public static ConfigEntry<KeyCode> InteractKey;
        public static ConfigEntry<KeyCode> DebugKey;
        public static ConfigEntry<KeyCode> PlaceTerminalKey;
        public static ConfigEntry<float> InteractRange;
        public static ConfigEntry<float> TerminalOffsetX;
        public static ConfigEntry<float> TerminalOffsetY;
        public static ConfigEntry<float> TerminalOffsetZ;
        public static ConfigEntry<float> TerminalRotationY;
        public static ConfigEntry<bool> TerminalPlacementSaved;

        public static Vector3 TerminalOffset =>
            new Vector3(TerminalOffsetX.Value, TerminalOffsetY.Value, TerminalOffsetZ.Value);

        private Harmony _harmony;
        private ChestWatchdog _watchdog;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ChestEnabled = Config.Bind("General", "ChestEnabled", true,
                "Spawn the storage chest at extraction points once they are completed.");
            TerminalEnabled = Config.Bind("General", "TerminalEnabled", true,
                "Spawn the wall terminal inside the truck.");
            TerminalInLevels = Config.Bind("General", "TerminalInLevels", true,
                "Also spawn the terminal in the truck while playing a level (not just in the lobby).");
            PurchasesGoToStorage = Config.Bind("General", "PurchasesGoToStorage", true,
                "Items bought in the shop are moved into the shared storage instead of spawning in the truck.");
            InteractKey = Config.Bind("Input", "InteractKey", KeyCode.E,
                "Key used to open the chest / terminal while looking at it.");
            DebugKey = Config.Bind("Input", "DebugKey", KeyCode.F8,
                "Dumps mod diagnostics into the BepInEx log and spawns a debug chest in front of the player.");
            PlaceTerminalKey = Config.Bind("Input", "PlaceTerminalKey", KeyCode.F9,
                "Places/moves the terminal onto the wall you are aiming at and saves that position.");
            InteractRange = Config.Bind("Input", "InteractRange", 2.6f,
                "Maximum distance to interact with the chest / terminal.");
            TerminalOffsetX = Config.Bind("Terminal", "OffsetX", 1.35f,
                "Terminal position offset (X, local space of the truck screen).");
            TerminalOffsetY = Config.Bind("Terminal", "OffsetY", 0f,
                "Terminal position offset (Y, local space of the truck screen).");
            TerminalOffsetZ = Config.Bind("Terminal", "OffsetZ", 0f,
                "Terminal position offset (Z, local space of the truck screen).");
            TerminalRotationY = Config.Bind("Terminal", "RotationY", 0f,
                "Additional Y rotation (degrees) applied to the terminal.");
            TerminalPlacementSaved = Config.Bind("Terminal", "PlacementSaved", false,
                "Set automatically once the terminal has been placed with the placement key. " +
                "While false, the terminal mounts itself onto the nearest wall around the truck screen.");

            Log.LogInfo("Config bound, applying Harmony patches...");
            _harmony = new Harmony(Guid);
            int patched = 0, failed = 0;
            foreach (Type type in typeof(Plugin).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    patched++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.LogError($"Patch class {type.Name} failed (game version mismatch?): {e.Message}");
                }
            }
            PatchesApplied = failed == 0;
            Log.LogInfo($"Harmony patching done: {patched} classes ok, {failed} failed.");

            NetworkEvents.Initialize();
            AssetFactory.TryLoadBundle();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Keep the plugin object alive across scene loads.
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<StorageMenu>();
            _watchdog = gameObject.AddComponent<ChestWatchdog>();

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Log.LogInfo($"Scene loaded: {scene.name}");
                ChestSpawner.OnSceneLoaded();
                ChestWatchdog.ResetOnSceneLoad(_watchdog);
                StorageMenu.ForceClose();
                if (TerminalEnabled.Value)
                {
                    StartCoroutine(TerminalSpawner.SpawnWhenReady());
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Scene hook failed: {e}");
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            NetworkEvents.Shutdown();
            _harmony?.UnpatchSelf();
        }
    }
}
