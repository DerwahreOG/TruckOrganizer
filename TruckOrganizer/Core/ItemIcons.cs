using System;
using System.Collections.Generic;
using UnityEngine;

namespace TruckOrganizer.Core
{
    /// <summary>
    /// Loads the same item icons the game shows in the inventory slots.
    /// They live on the item prefabs (ItemEquippable.ItemIcon, fallback
    /// ItemAttributes.icon) and are cached per item name.
    /// </summary>
    public static class ItemIcons
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string itemName)
        {
            if (_cache.TryGetValue(itemName, out Sprite cached)) return cached;

            Sprite sprite = null;
            try
            {
                Item item = StorageService.ResolveItem(itemName);
                GameObject prefab = item != null && item.prefab != null ? item.prefab.Prefab : null;
                if (prefab != null)
                {
                    ItemEquippable equippable = prefab.GetComponentInChildren<ItemEquippable>(true);
                    if (equippable != null) sprite = equippable.ItemIcon;

                    if (sprite == null)
                    {
                        ItemAttributes attributes = prefab.GetComponentInChildren<ItemAttributes>(true);
                        if (attributes != null) sprite = attributes.icon;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not load icon for '{itemName}': {e.Message}");
            }

            _cache[itemName] = sprite;
            return sprite;
        }

        /// <summary>Draws the sprite into the given rect (IMGUI).</summary>
        public static void Draw(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            if (Event.current.type != EventType.Repaint) return;

            try
            {
                Texture2D tex = sprite.texture;
                Rect tr;
                try { tr = sprite.textureRect; }
                catch { tr = sprite.rect; } // tightly packed sprites

                var coords = new Rect(tr.x / tex.width, tr.y / tex.height,
                    tr.width / tex.width, tr.height / tex.height);
                GUI.DrawTextureWithTexCoords(rect, tex, coords, true);
            }
            catch
            {
                GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
            }
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
