using UnityEngine;

namespace BomberGhst
{
    /// A tiny CPU raster canvas. Every sprite in the game is drawn here at
    /// runtime, so the project ships with no binary art assets at all.
    public class Pix
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public Pix(int w, int h)
        {
            W = w; H = h;
            Px = new Color32[w * h];
        }

        bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        /// Writes only when the source is opaque (pixel art: no blending).
        public void Set(int x, int y, Color32 c)
        {
            if (!In(x, y) || c.a == 0) return;
            Px[y * W + x] = c;
        }

        public void Put(int x, int y, Color32 c)
        {
            if (!In(x, y)) return;
            Px[y * W + x] = c;
        }

        public Color32 Get(int x, int y) => In(x, y) ? Px[y * W + x] : Pal.Clear;

        public void Fill(Color32 c) { for (int i = 0; i < Px.Length; i++) Px[i] = c; }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++) Set(x + i, y + j, c);
        }

        public void Cut(int x, int y, int w, int h)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++) Put(x + i, y + j, Pal.Clear);
        }

        public void Frame(int x, int y, int w, int h, Color32 c)
        {
            Rect(x, y, w, 1, c); Rect(x, y + h - 1, w, 1, c);
            Rect(x, y, 1, h, c); Rect(x + w - 1, y, 1, h, c);
        }

        public void Disc(float cx, float cy, float r, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r2) Set(x, y, c);
                }
        }

        public void DiscCut(float cx, float cy, float r)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r2) Put(x, y, Pal.Clear);
                }
        }

        /// Axis-aligned filled ellipse.
        public void Oval(float cx, float cy, float rx, float ry, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        /// One pixel dark border around every opaque pixel.
        public void Outline(Color32 c)
        {
            var src = (Color32[])Px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (src[y * W + x].a != 0) continue;
                    bool touch =
                        (x > 0 && src[y * W + x - 1].a != 0) ||
                        (x < W - 1 && src[y * W + x + 1].a != 0) ||
                        (y > 0 && src[(y - 1) * W + x].a != 0) ||
                        (y < H - 1 && src[(y + 1) * W + x].a != 0);
                    if (touch) Put(x, y, c);
                }
        }

        /// Knocks out pixels pseudo-randomly; used for the crumble frames.
        public void Dissolve(float amount, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < Px.Length; i++)
                if (Px[i].a != 0 && rng.NextDouble() < amount) Px[i] = Pal.Clear;
        }

        /// Stamps a small string-art icon. '.' is transparent, any other char
        /// is looked up in <paramref name="map"/>. Rows are given top-down.
        public void Stamp(int x, int y, string[] rows, System.Collections.Generic.Dictionary<char, Color32> map)
        {
            for (int j = 0; j < rows.Length; j++)
            {
                string row = rows[rows.Length - 1 - j];
                for (int i = 0; i < row.Length; i++)
                {
                    char ch = row[i];
                    if (ch == '.' || ch == ' ') continue;
                    if (map.TryGetValue(ch, out var c)) Set(x + i, y + j, c);
                }
            }
        }

        public Texture2D ToTexture()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels32(Px);
            tex.Apply();
            return tex;
        }

        public Sprite ToSprite(float pivotX = 0.5f, float pivotY = 0.5f, int ppu = Config.PPU)
        {
            var sp = Sprite.Create(ToTexture(), new Rect(0, 0, W, H),
                new Vector2(pivotX, pivotY), ppu, 0, SpriteMeshType.FullRect);
            sp.hideFlags = HideFlags.HideAndDontSave;
            return sp;
        }
    }
}
