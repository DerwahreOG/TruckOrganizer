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
        public const string Version = "0.1.0";

        public static Plugin Instance { get; private set; }
        internal static ManualLogSource Log;

        // --- Config ---
        public static ConfigEntry<bool> ChestEnabled;
        public static ConfigEntry<bool> TerminalEnabled;
        public static ConfigEntry<bool> TerminalInLevels;
        public static ConfigEntry<bool> PurchasesGoToStorage;
        public static ConfigEntry<KeyCode> InteractKey;
        public static ConfigEntry<float> InteractRange;
        public static ConfigEntry<Vector3> TerminalOffset;
        public static ConfigEntry<Vector3> TerminalEulerOffset;

        private Harmony _harmony;

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
            InteractRange = Config.Bind("Input", "InteractRange", 2.6f,
                "Maximum distance to interact with the chest / terminal.");
            TerminalOffset = Config.Bind("Terminal", "PositionOffset", new Vector3(1.35f, 0.0f, 0.0f),
                "Position offset of the terminal relative to the truck screen (local space of the screen).");
            TerminalEulerOffset = Config.Bind("Terminal", "RotationOffset", Vector3.zero,
                "Additional rotation (euler angles) applied to the terminal.");

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            NetworkEvents.Initialize();
            AssetFactory.TryLoadBundle();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Keep the plugin object alive across scene loads.
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<StorageMenu>();

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ChestSpawner.OnSceneLoaded();
            StorageMenu.ForceClose();
            if (TerminalEnabled.Value)
            {
                StartCoroutine(TerminalSpawner.SpawnWhenReady());
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
