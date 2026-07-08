using System.Collections.Generic;
using System.Linq;
using TruckOrganizer.Core;
using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// Storage UI shared by chest and terminal, drawn in a dark CRT-green
    /// theme. Lists the storage content, lets players take items into their
    /// inventory slots or consume player upgrades directly. Fully keyboard
    /// operable (arrow keys + Enter/U) in addition to the mouse.
    /// </summary>
    public class StorageMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public static StorageContainer HintSource;

        private static StorageContainer _source;
        private static Vector2 _scroll;
        private static int _selected;
        private static int _closedFrame = -1;
        private static float _relockUntil = -1f;

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
            if (IsOpen)
            {
                _closedFrame = Time.frameCount;
                // Hand the cursor back to the game: keep enforcing the lock
                // for a while, because the game does not reliably re-lock on
                // its own. Game-owned menus (unlockTimer) are respected.
                _relockUntil = Time.time + 10f;
            }
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

            // Click diagnostics: shows in the log whether mouse input reaches
            // the mod at all while the menu is open.
            if (Input.GetMouseButtonDown(0))
            {
                Plugin.Log.LogInfo($"Menu click at {Input.mousePosition}, cursor lock: {Cursor.lockState}.");
            }

            HandleKeyboard();
            HoldGameInput();
        }

        private void LateUpdate()
        {
            // Runs after the game's own cursor handling; last writer wins.
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Time.time < _relockUntil)
            {
                // Stop while a game-owned menu wants the cursor (e.g. Esc
                // menu uses CursorManager.Unlock), or outside gameplay.
                bool gameCursorWantsUnlock = false;
                try
                {
                    gameCursorWantsUnlock = CursorManager.instance != null &&
                                            CursorManager.instance.unlockTimer > 0.05f;
                }
                catch { /* manager not available */ }

                bool inGameplay = true;
                try
                {
                    inGameplay = !SemiFunc.MenuLevel();
                }
                catch { /* keep true */ }

                if (!gameCursorWantsUnlock && inGameplay &&
                    Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
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
            UITheme.Ensure();

            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                DrawPanel();
            }
            else if (HintSource != null)
            {
                DrawHint();
            }
        }

        private static void DrawHint()
        {
            string text = $"[{Plugin.InteractKey.Value}]  {HintSource.label} öffnen";
            var size = new Vector2(320f, 34f);
            var rect = new Rect((Screen.width - size.x) / 2f, Screen.height * 0.72f, size.x, size.y);
            GUI.Label(rect, text, UITheme.PromptBox);
        }

        private static void DrawPanel()
        {
            List<Row> rows = BuildRows();
            _selected = rows.Count == 0 ? 0 : Mathf.Clamp(_selected, 0, rows.Count - 1);
            int totalItems = rows.Sum(r => r.Count);

            float width = 600f;
            float height = 500f;
            var panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            UITheme.DrawPanelFrame(panel);

            // --- Header ---
            var header = new Rect(panel.x + 20f, panel.y + 6f, panel.width - 40f, 40f);
            GUI.Label(header, _source != null ? _source.label.ToUpperInvariant() : "LAGER", UITheme.Title);
            GUI.Label(header, $"{totalItems} Gegenstände eingelagert", UITheme.TitleRight);

            // --- Content ---
            var content = new Rect(panel.x + 12f, panel.y + 52f, panel.width - 24f, panel.height - 52f - 92f);
            GUILayout.BeginArea(content);
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (rows.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("— DAS LAGER IST LEER —", UITheme.Empty);
                GUILayout.Label("Im Shop gekaufte Items landen automatisch hier.", UITheme.Hint);
                GUILayout.FlexibleSpace();
            }
            else
            {
                bool upgradeHeader = false;
                bool itemHeader = false;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].IsUpgrade && !upgradeHeader)
                    {
                        GUILayout.Space(2f);
                        GUILayout.Label("── UPGRADES ──", UITheme.Section);
                        upgradeHeader = true;
                    }
                    if (!rows[i].IsUpgrade && !itemHeader)
                    {
                        if (upgradeHeader) GUILayout.Space(10f);
                        GUILayout.Label("── ITEMS ──", UITheme.Section);
                        itemHeader = true;
                    }
                    DrawRow(rows[i], i);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // --- Footer ---
            var footer = new Rect(panel.x + 12f, panel.yMax - 88f, panel.width - 24f, 80f);
            GUILayout.BeginArea(footer);
            if (!string.IsNullOrEmpty(StorageService.LastStatus))
            {
                GUILayout.Label(StorageService.LastStatus, UITheme.Section);
            }
            GUILayout.Label(
                "↑/↓ wählen   ·   Enter = Nehmen   ·   U = Upgrade benutzen   ·   " +
                $"{Plugin.InteractKey.Value}/Tab = schließen", UITheme.Hint);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SCHLIESSEN", UITheme.Button, GUILayout.Width(140f), GUILayout.Height(28f)))
            {
                ForceClose();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawRow(Row row, int index)
        {
            GUIStyle rowStyle = index == _selected
                ? UITheme.RowSel
                : (index % 2 == 0 ? UITheme.Row : UITheme.RowAlt);

            GUILayout.BeginHorizontal(rowStyle, GUILayout.Height(34f));

            // Same icon the game shows in the inventory slots.
            Rect iconRect = GUILayoutUtility.GetRect(28f, 28f, GUILayout.Width(28f), GUILayout.Height(28f));
            ItemIcons.Draw(iconRect, ItemIcons.Get(row.ItemName));
            GUILayout.Space(8f);

            GUILayout.Label(StorageService.DisplayName(row.ItemName), UITheme.RowLabel,
                GUILayout.ExpandWidth(true), GUILayout.Height(24f));
            GUILayout.Label($"x{row.Count}", UITheme.RowCount, GUILayout.Width(44f), GUILayout.Height(24f));
            GUILayout.Space(8f);

            if (row.IsUpgrade)
            {
                if (GUILayout.Button("BENUTZEN", UITheme.Button, GUILayout.Width(96f), GUILayout.Height(24f)))
                {
                    _selected = index;
                    StorageService.RequestUse(row.ItemName);
                }
                GUILayout.Space(4f);
            }

            if (GUILayout.Button("NEHMEN", UITheme.Button, GUILayout.Width(96f), GUILayout.Height(24f)))
            {
                _selected = index;
                StorageService.RequestTake(row.ItemName);
                ForceClose();
            }

            GUILayout.EndHorizontal();

            // Mouse hover moves the keyboard selection along.
            if (Event.current.type == EventType.Repaint &&
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                _selected = index;
            }

            GUILayout.Space(2f);
        }
    }
}
