using System.Collections.Generic;
using System.Linq;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// IMGUI menu shared by chest and terminal. Lists the storage content,
    /// lets players take items into their inventory slots or consume player
    /// upgrades directly.
    /// </summary>
    public class StorageMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public static StorageContainer HintSource;

        private static StorageContainer _source;
        private static Vector2 _scroll;

        private const int WindowId = 0x7402;

        public static void Open(StorageContainer source)
        {
            _source = source;
            IsOpen = true;
            StorageService.RequestSnapshot();
        }

        public static void ForceClose()
        {
            IsOpen = false;
            _source = null;
            HintSource = null;
        }

        public static void CloseIfSource(StorageContainer source)
        {
            if (_source == source) ForceClose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(Plugin.DebugKey.Value))
            {
                try
                {
                    Core.Diagnostics.DumpAndSpawnDebugChest();
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"Diagnostics failed: {e}");
                }
            }

            if (!IsOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                ForceClose();
                return;
            }

            if (_source == null || !_source.LocalPlayerCanInteract())
            {
                ForceClose();
                return;
            }

            HoldGameInput();
        }

        private static void HoldGameInput()
        {
            try
            {
                if (CursorManager.instance != null) CursorManager.instance.Unlock(0.2f);
                if (InputManager.instance != null)
                {
                    InputManager.instance.disableMovementTimer = 0.2f;
                    InputManager.instance.disableAimingTimer = 0.2f;
                }
            }
            catch { /* game managers not available in this scene */ }
        }

        private void OnGUI()
        {
            if (IsOpen)
            {
                float width = 460f;
                float height = 420f;
                var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
                GUILayout.Window(WindowId, rect, DrawWindow, _source != null ? _source.label : "Lager");
            }
            else if (HintSource != null && !IsOpen)
            {
                DrawHint();
            }
        }

        private static void DrawHint()
        {
            string text = $"[{Plugin.InteractKey.Value}] {HintSource.label} öffnen";
            var size = new Vector2(280f, 28f);
            var rect = new Rect((Screen.width - size.x) / 2f, Screen.height * 0.72f, size.x, size.y);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.6f, 1f, 0.7f);
            GUI.Label(rect, text, CenteredLabel());
            GUI.color = Color.white;
        }

        private static void DrawWindow(int id)
        {
            List<KeyValuePair<string, int>> upgrades = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();

            foreach (var entry in StorageService.Contents.OrderBy(e => StorageService.DisplayName(e.Key)))
            {
                if (entry.Value <= 0) continue;
                if (StorageService.IsPlayerUpgrade(entry.Key)) upgrades.Add(entry);
                else items.Add(entry);
            }

            _scroll = GUILayout.BeginScrollView(_scroll);

            if (upgrades.Count == 0 && items.Count == 0)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Das Lager ist leer.", CenteredLabel());
            }

            if (upgrades.Count > 0)
            {
                GUILayout.Label("<b>Upgrades</b>", RichLabel());
                foreach (var entry in upgrades) DrawRow(entry, isUpgrade: true);
                GUILayout.Space(8f);
            }

            if (items.Count > 0)
            {
                GUILayout.Label("<b>Items</b>", RichLabel());
                foreach (var entry in items) DrawRow(entry, isUpgrade: false);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Schließen [Esc]"))
            {
                ForceClose();
            }
        }

        private static void DrawRow(KeyValuePair<string, int> entry, bool isUpgrade)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{StorageService.DisplayName(entry.Key)}  x{entry.Value}", GUILayout.ExpandWidth(true));

            if (isUpgrade)
            {
                if (GUILayout.Button("Benutzen", GUILayout.Width(90f)))
                {
                    StorageService.RequestUse(entry.Key);
                }
            }

            if (GUILayout.Button("Nehmen", GUILayout.Width(90f)))
            {
                StorageService.RequestTake(entry.Key);
                ForceClose();
            }

            GUILayout.EndHorizontal();
        }

        private static GUIStyle _centeredLabel;
        private static GUIStyle _richLabel;

        private static GUIStyle CenteredLabel()
        {
            return _centeredLabel ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };
        }

        private static GUIStyle RichLabel()
        {
            return _richLabel ??= new GUIStyle(GUI.skin.label)
            {
                richText = true,
            };
        }
    }
}
