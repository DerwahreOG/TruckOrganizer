using System.Collections.Generic;
using System.Linq;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// IMGUI menu shared by chest and terminal. Lists the storage content,
    /// lets players take items into their inventory slots or consume player
    /// upgrades directly. Fully keyboard-operable (arrow keys + Enter/U) in
    /// case the game fights over the mouse cursor.
    /// </summary>
    public class StorageMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public static StorageContainer HintSource;

        private static StorageContainer _source;
        private static Vector2 _scroll;
        private static int _selected;
        private static int _closedFrame = -1;

        private const int WindowId = 0x7402;

        private struct Row
        {
            public string ItemName;
            public int Count;
            public bool IsUpgrade;
        }

        public static void Open(StorageContainer source)
        {
            // Do not reopen in the very frame the menu was closed with the
            // interact key, otherwise close/open toggle in one frame.
            if (Time.frameCount == _closedFrame) return;
            _source = source;
            _selected = 0;
            IsOpen = true;
            StorageService.RequestSnapshot();
        }

        public static void ForceClose()
        {
            if (IsOpen) _closedFrame = Time.frameCount;
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
                    Diagnostics.DumpAndSpawnDebugChest();
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"Diagnostics failed: {e}");
                }
            }

            if (Input.GetKeyDown(Plugin.PlaceTerminalKey.Value))
            {
                try
                {
                    TerminalSpawner.PlaceAtAim();
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"Terminal placement failed: {e}");
                }
            }

            if (!IsOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) ||
                Input.GetKeyDown(Plugin.InteractKey.Value))
            {
                ForceClose();
                return;
            }

            if (_source == null || !_source.LocalPlayerWithinRange())
            {
                ForceClose();
                return;
            }

            HandleKeyboard();
            HoldGameInput();
        }

        private void LateUpdate()
        {
            // Runs after the game's own cursor handling; last writer wins.
            if (!IsOpen) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void HandleKeyboard()
        {
            List<Row> rows = BuildRows();
            if (rows.Count == 0) return;

            _selected = Mathf.Clamp(_selected, 0, rows.Count - 1);

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                _selected = (_selected + 1) % rows.Count;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _selected = (_selected - 1 + rows.Count) % rows.Count;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StorageService.RequestTake(rows[_selected].ItemName);
                ForceClose();
            }
            else if (Input.GetKeyDown(KeyCode.U) && rows[_selected].IsUpgrade)
            {
                StorageService.RequestUse(rows[_selected].ItemName);
            }
        }

        private static void HoldGameInput()
        {
            try
            {
                if (CursorManager.instance != null) CursorManager.instance.Unlock(0.25f);
                if (InputManager.instance != null)
                {
                    InputManager.instance.disableMovementTimer = 0.25f;
                    InputManager.instance.disableAimingTimer = 0.25f;
                }
            }
            catch { /* game managers not available in this scene */ }
        }

        private static List<Row> BuildRows()
        {
            var rows = new List<Row>();
            foreach (var entry in StorageService.Contents
                         .Where(e => e.Value > 0)
                         .OrderBy(e => !StorageService.IsPlayerUpgrade(e.Key))
                         .ThenBy(e => StorageService.DisplayName(e.Key)))
            {
                rows.Add(new Row
                {
                    ItemName = entry.Key,
                    Count = entry.Value,
                    IsUpgrade = StorageService.IsPlayerUpgrade(entry.Key),
                });
            }
            return rows;
        }

        private void OnGUI()
        {
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                float width = 520f;
                float height = 440f;
                var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
                GUILayout.Window(WindowId, rect, DrawWindow, _source != null ? _source.label : "Lager");
            }
            else if (HintSource != null)
            {
                DrawHint();
            }
        }

        private static void DrawHint()
        {
            string text = $"[{Plugin.InteractKey.Value}] {HintSource.label} öffnen";
            var size = new Vector2(300f, 28f);
            var rect = new Rect((Screen.width - size.x) / 2f, Screen.height * 0.72f, size.x, size.y);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.6f, 1f, 0.7f);
            GUI.Label(rect, text, CenteredLabel());
            GUI.color = Color.white;
        }

        private static void DrawWindow(int id)
        {
            List<Row> rows = BuildRows();
            _selected = rows.Count == 0 ? 0 : Mathf.Clamp(_selected, 0, rows.Count - 1);

            GUILayout.Label(
                "Steuerung: ↑/↓ wählen, Enter = Nehmen, U = Upgrade benutzen, " +
                $"{Plugin.InteractKey.Value}/Tab/Esc = schließen", SmallLabel());
            GUILayout.Space(4f);

            _scroll = GUILayout.BeginScrollView(_scroll);

            if (rows.Count == 0)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Das Lager ist leer.", CenteredLabel());
            }

            bool headerDrawn = false;
            for (int i = 0; i < rows.Count; i++)
            {
                if (i == 0 && rows[i].IsUpgrade)
                {
                    GUILayout.Label("<b>Upgrades</b>", RichLabel());
                }
                if (!rows[i].IsUpgrade && !headerDrawn)
                {
                    if (i > 0) GUILayout.Space(8f);
                    GUILayout.Label("<b>Items</b>", RichLabel());
                    headerDrawn = true;
                }
                DrawRow(rows[i], i);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Schließen"))
            {
                ForceClose();
            }
        }

        private static void DrawRow(Row row, int index)
        {
            GUILayout.BeginHorizontal();

            string marker = index == _selected ? "► " : "    ";
            GUILayout.Label($"{marker}{StorageService.DisplayName(row.ItemName)}  x{row.Count}",
                GUILayout.ExpandWidth(true));

            if (row.IsUpgrade)
            {
                if (GUILayout.Button("Benutzen [U]", GUILayout.Width(110f)))
                {
                    _selected = index;
                    StorageService.RequestUse(row.ItemName);
                }
            }

            if (GUILayout.Button("Nehmen [Enter]", GUILayout.Width(120f)))
            {
                _selected = index;
                StorageService.RequestTake(row.ItemName);
                ForceClose();
            }

            GUILayout.EndHorizontal();
        }

        private static GUIStyle _centeredLabel;
        private static GUIStyle _richLabel;
        private static GUIStyle _smallLabel;

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

        private static GUIStyle SmallLabel()
        {
            return _smallLabel ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
            };
        }
    }
}
