using UnityEngine;

namespace BomberGhst
{
    /// Convenience wrapper: a GameObject that shows a line of pixel text.
    public class Label : MonoBehaviour
    {
        SpriteRenderer sr;
        string current;
        Color32 currentColor = Pal.White;
        bool shadow = true;

        public static Label Create(Transform parent, string name, int sortingOrder, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var lb = go.AddComponent<Label>();
            lb.sr = go.AddComponent<SpriteRenderer>();
            lb.sr.sortingOrder = sortingOrder;
            return lb;
        }

        public SpriteRenderer Renderer => sr;

        public void Set(string text, Color32 color, bool withShadow = true)
        {
            if (text == current && ColorEq(color, currentColor) && withShadow == shadow) return;
            current = text; currentColor = color; shadow = withShadow;
            sr.sprite = PixelFont.Text(text, color, withShadow);
        }

        public void SetVisible(bool v) { if (sr != null) sr.enabled = v; }

        static bool ColorEq(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
    }
}
