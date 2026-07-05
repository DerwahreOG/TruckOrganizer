using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// Runtime-built dark/CRT-green theme for the storage UI. All textures
    /// are generated in code so no asset bundle is required.
    /// </summary>
    internal static class UITheme
    {
        public static readonly Color Green = new Color(0.10f, 1.00f, 0.35f);
        public static readonly Color GreenDim = new Color(0.49f, 1.00f, 0.66f);
        public static readonly Color TextMain = new Color(0.81f, 0.96f, 0.86f);

        public static Texture2D Dim;
        public static Texture2D Panel;
        public static Texture2D Border;
        public static Texture2D HeaderBar;
        public static Texture2D RowEven;
        public static Texture2D RowOdd;
        public static Texture2D RowSelected;
        public static Texture2D BtnNormal;
        public static Texture2D BtnHover;
        public static Texture2D BtnActive;

        public static GUIStyle Title;
        public static GUIStyle TitleRight;
        public static GUIStyle Section;
        public static GUIStyle RowLabel;
        public static GUIStyle RowCount;
        public static GUIStyle Button;
        public static GUIStyle Hint;
        public static GUIStyle Empty;
        public static GUIStyle PromptBox;
        public static GUIStyle Row;
        public static GUIStyle RowAlt;
        public static GUIStyle RowSel;

        private static bool _ready;

        public static void Ensure()
        {
            if (_ready && Panel != null) return;

            Dim = Solid(new Color(0f, 0f, 0f, 0.62f));
            Panel = Solid(new Color(0.045f, 0.075f, 0.055f, 0.97f));
            Border = Solid(new Color(0.10f, 1.00f, 0.35f, 0.85f));
            HeaderBar = Solid(new Color(0.07f, 0.16f, 0.10f, 0.95f));
            RowEven = Solid(new Color(1f, 1f, 1f, 0.035f));
            RowOdd = Solid(new Color(1f, 1f, 1f, 0f));
            RowSelected = Solid(new Color(0.10f, 1.00f, 0.35f, 0.16f));
            BtnNormal = Solid(new Color(0.09f, 0.22f, 0.13f, 0.95f));
            BtnHover = Solid(new Color(0.13f, 0.34f, 0.19f, 0.95f));
            BtnActive = Solid(new Color(0.10f, 1.00f, 0.35f, 0.35f));

            Title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            Title.normal.textColor = Green;

            TitleRight = new GUIStyle(Title)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleRight,
            };
            TitleRight.normal.textColor = GreenDim;

            Section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            Section.normal.textColor = GreenDim;

            RowLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
            };
            RowLabel.normal.textColor = TextMain;

            RowCount = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
            RowCount.normal.textColor = Green;

            Button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 5, 5),
            };
            Button.normal.background = BtnNormal;
            Button.normal.textColor = TextMain;
            Button.hover.background = BtnHover;
            Button.hover.textColor = Color.white;
            Button.active.background = BtnActive;
            Button.active.textColor = Color.white;
            Button.focused.background = BtnNormal;
            Button.focused.textColor = TextMain;

            Hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
            };
            Hint.normal.textColor = new Color(0.55f, 0.75f, 0.62f);

            Empty = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
            };
            Empty.normal.textColor = new Color(0.55f, 0.75f, 0.62f);

            PromptBox = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            PromptBox.normal.textColor = Green;
            PromptBox.normal.background = Solid(new Color(0f, 0f, 0f, 0.72f));

            Row = new GUIStyle { normal = { background = RowEven }, padding = new RectOffset(8, 8, 4, 4) };
            RowAlt = new GUIStyle { normal = { background = RowOdd }, padding = new RectOffset(8, 8, 4, 4) };
            RowSel = new GUIStyle { normal = { background = RowSelected }, padding = new RectOffset(8, 8, 4, 4) };

            _ready = true;
        }

        public static void DrawPanelFrame(Rect panel)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Dim);
            GUI.DrawTexture(new Rect(panel.x - 2f, panel.y - 2f, panel.width + 4f, panel.height + 4f), Border);
            GUI.DrawTexture(panel, Panel);
            // Header strip behind the title area.
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 44f), HeaderBar);
            // Thin divider under the header.
            GUI.DrawTexture(new Rect(panel.x, panel.y + 44f, panel.width, 1f), Border);
        }

        private static Texture2D Solid(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
