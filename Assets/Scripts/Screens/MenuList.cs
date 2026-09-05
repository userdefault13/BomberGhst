using System;
using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst.Screens
{
    public class MenuItem
    {
        public string Label;
        public Action Activate;
        public bool Enabled = true;
        public Color32 Tint = Pal.White;

        public MenuItem(string label, Action activate, bool enabled = true)
        {
            Label = label; Activate = activate; Enabled = enabled;
        }
    }

    /// Keyboard menu with a blinking caret, drawn in the pixel font.
    public class MenuList : MonoBehaviour
    {
        readonly List<Label> rows = new List<Label>();
        readonly List<MenuItem> items = new List<MenuItem>();
        SpriteRenderer caret;
        float originX, originY, spacing;
        int index;

        public static MenuList Create(Transform parent, float px, float py, float spacing, int maxRows, int order)
        {
            var go = new GameObject("Menu");
            go.transform.SetParent(parent, false);
            var menu = go.AddComponent<MenuList>();
            menu.originX = px; menu.originY = py; menu.spacing = spacing;

            for (int i = 0; i < maxRows; i++)
            {
                var lb = Label.Create(go.transform, "Row" + i, order, 1f);
                lb.transform.position = new Vector3(px / Config.PPU, (py - i * spacing) / Config.PPU, 0f);
                lb.SetVisible(false);
                menu.rows.Add(lb);
            }

            var caretGo = new GameObject("Caret");
            caretGo.transform.SetParent(go.transform, false);
            menu.caret = caretGo.AddComponent<SpriteRenderer>();
            menu.caret.sprite = Art.Caret();
            menu.caret.sortingOrder = order;
            return menu;
        }

        public MenuItem Selected => index >= 0 && index < items.Count ? items[index] : null;

        public void Set(List<MenuItem> next)
        {
            items.Clear();
            items.AddRange(next);
            if (index >= items.Count) index = Mathf.Max(0, items.Count - 1);
            EnsureSelectable(1);
            Render();
        }

        void EnsureSelectable(int direction)
        {
            for (int guard = 0; guard < items.Count && items.Count > 0; guard++)
            {
                if (items[index].Enabled) return;
                index = (index + direction + items.Count) % items.Count;
            }
        }

        void Move(int direction)
        {
            if (items.Count == 0) return;
            int start = index;
            do
            {
                index = (index + direction + items.Count) % items.Count;
                if (items[index].Enabled) break;
            } while (index != start);
            Sfx.Play(Sound.Menu);
            Render();
        }

        /// Call from the owning screen's Update.
        public void Tick()
        {
            if (items.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Move(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Move(1);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                var item = Selected;
                if (item != null && item.Enabled)
                {
                    Sfx.Play(Sound.Pickup);
                    item.Activate?.Invoke();
                }
            }

            if (caret != null && caret.enabled)
                caret.color = new Color(1f, 1f, 1f, Mathf.Sin(Time.unscaledTime * 6f) > -0.3f ? 1f : 0.25f);
        }

        void Render()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= items.Count) { rows[i].SetVisible(false); continue; }
                var item = items[i];
                rows[i].SetVisible(true);
                var tint = !item.Enabled ? (Color32)Pal.Gray : (i == index ? (Color32)Pal.Yellow : item.Tint);
                rows[i].Set(item.Label, tint);
            }

            if (caret == null) return;
            caret.enabled = items.Count > 0;
            caret.transform.position = new Vector3(
                (originX - RowHalfWidth(index) - 8f) / Config.PPU,
                (originY - index * spacing) / Config.PPU, 0f);
        }

        float RowHalfWidth(int i)
        {
            if (i < 0 || i >= rows.Count) return 0f;
            var sr = rows[i].Renderer;
            return sr != null && sr.sprite != null ? sr.sprite.rect.width * 0.5f : 0f;
        }

        void LateUpdate()
        {
            // widths are only known after the label sprite is built
            if (caret != null && caret.enabled)
                caret.transform.position = new Vector3(
                    (originX - RowHalfWidth(index) - 8f) / Config.PPU,
                    (originY - index * spacing) / Config.PPU, 0f);
        }
    }
}
