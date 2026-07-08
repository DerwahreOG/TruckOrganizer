using UnityEngine;

namespace TruckOrganizer.Behaviours
{
    /// <summary>
    /// Runtime-built UI theme styled after the game's menus: dark rounded
    /// panels with warm orange accents and clean white text. All textures are
    /// generated in code so no asset bundle is required.
    /// </summary>
    internal static class UITheme
    {
        public static readonly Color Accent = new Color(1.00f, 0.62f, 0.22f);
        public static readonly Color AccentSoft = new Color(1.00f, 0.76f, 0.45f);
        public static readonly Color TextMain = new Color(0.95f, 0.94f, 0.92f);
        public static readonly Color TextDim = new Color(0.65f, 0.63f, 0.66f);

        public static Texture2D Dim;

        public static GUIStyle PanelBox;
        public static GUIStyle BorderBox;
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
            if (_ready && Dim != null) return;

            Dim = Solid(new Color(0f, 0f, 0f, 0.6f));

            Texture2D panelTex = Rounded(64, 16, new Color(0.075f, 0.06f, 0.09f, 0.97f));
            Texture2D borderTex = Rounded(64, 18, new Color(Accent.r, Accent.g, Accent.b, 0.9f));
            Texture2D rowTex = Rounded(32, 8, new Color(1f, 1f, 1f, 0.045f));
            Texture2D rowSelTex = Rounded(32, 8, new Color(Accent.r, Accent.g, Accent.b, 0.22f));
            Texture2D btnTex = Rounded(32, 8, new Color(0.22f, 0.15f, 0.10f, 0.95f));
            Texture2D btnHoverTex = Rounded(32, 8, new Color(0.45f, 0.28f, 0.12f, 0.95f));
            Texture2D btnActiveTex = Rounded(32, 8, new Color(Accent.r, Accent.g, Accent.b, 0.55f));
            Texture2D promptTex = Rounded(32, 10, new Color(0.05f, 0.04f, 0.06f, 0.85f));

            PanelBox = BoxStyle(panelTex, 18);
            BorderBox = BoxStyle(borderTex, 20);

            Title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            Title.normal.textColor = TextMain;

            TitleRight = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
            };
            TitleRight.normal.textColor = AccentSoft;

            Section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            Section.normal.textColor = Accent;

            RowLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
            };
            RowLabel.normal.textColor = TextMain;

            RowCount = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
            RowCount.normal.textColor = Accent;

            Button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 6, 6),
                border = new RectOffset(10, 10, 10, 10),
            };
            Button.normal.background = btnTex;
            Button.normal.textColor = TextMain;
            Button.hover.background = btnHoverTex;
            Button.hover.textColor = Color.white;
            Button.active.background = btnActiveTex;
            Button.active.textColor = Color.white;
            Button.focused.background = btnTex;
            Button.focused.textColor = TextMain;

            Hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
            };
            Hint.normal.textColor = TextDim;

            Empty = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
            };
            Empty.normal.textColor = TextDim;

            PromptBox = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(12, 12, 12, 12),
            };
            PromptBox.normal.textColor = Accent;
            PromptBox.normal.background = promptTex;

            Row = RowStyle(rowTex);
            RowAlt = RowStyle(null);
            RowSel = RowStyle(rowSelTex);

            _ready = true;
        }

        public static void DrawPanelFrame(Rect panel)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Dim);
            GUI.Box(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f),
                GUIContent.none, BorderBox);
            GUI.Box(panel, GUIContent.none, PanelBox);
        }

        // ------------------------------------------------------------------
        // Texture / style builders
        // ------------------------------------------------------------------

        private static GUIStyle BoxStyle(Texture2D tex, int slice)
        {
            var style = new GUIStyle
            {
                border = new RectOffset(slice, slice, slice, slice),
            };
            style.normal.background = tex;
            return style;
        }

        private static GUIStyle RowStyle(Texture2D bg)
        {
            var style = new GUIStyle
            {
                padding = new RectOffset(10, 10, 5, 5),
                border = new RectOffset(8, 8, 8, 8),
            };
            if (bg != null) style.normal.background = bg;
            return style;
        }

        private static Texture2D Solid(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        /// <summary>Square texture with rounded corners for 9-sliced styles.</summary>
        private static Texture2D Rounded(int size, int radius, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, InsideRounded(x, y, size, radius) ? fill : clear);
                }
            }

            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static bool InsideRounded(int x, int y, int size, int radius)
        {
            int max = size - 1;
            // Determine the nearest corner circle center, if in a corner square.
            int cx = x < radius ? radius : (x > max - radius ? max - radius : -1);
            int cy = y < radius ? radius : (y > max - radius ? max - radius : -1);
            if (cx < 0 || cy < 0) return true;

            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
