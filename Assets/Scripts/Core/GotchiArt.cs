using System;
using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    /// Bomber sprites built from Aavegotchi art. tools/build_gotchi_sprites.py
    /// composites the Aavegotchi Paaint part PNGs into Resources/GotchiSprites,
    /// one row per player, one column per pose. When that sheet is missing the
    /// game falls back to the procedural ghost, so it still runs standalone.
    public static class GotchiArt
    {
        public const int Cell = 32;
        public const int Cols = 6;

        // column order must match FRAMES in the exporter
        const int ColDownOpen = 0;
        const int ColDownClosed = 1;
        const int ColUpOpen = 2;
        const int ColUpClosed = 3;
        const int ColSide = 4;
        const int ColDead = 5;

        /// Feet sit this many pixels above the bottom of a cell, so the pivot
        /// can be put on the ground rather than in the middle of the body.
        const float FeetPixels = 4f;

        [Serializable]
        class Row
        {
            public int row;
            public string collateral;
            public string accent;
        }

        [Serializable]
        class Manifest
        {
            public int cell;
            public string[] defaultSlots;
            public Row[] rows;
        }

        static Sprite[,] cache;
        static readonly Dictionary<string, int> rowByCollateral =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        static string[] defaultSlots = { "mauni", "mayfi", "mausdt", "madai" };
        static bool loaded;
        static bool available;

        public static bool Available
        {
            get { Load(); return available; }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;

            var tex = Resources.Load<Texture2D>("GotchiSprites");
            if (tex == null) return;

            int rows = tex.height / Cell;
            if (tex.width < Cols * Cell || rows < 1)
            {
                Debug.LogWarning($"[GotchiArt] GotchiSprites is {tex.width}x{tex.height}, " +
                                 $"expected at least {Cols * Cell} wide; using the drawn ghost.");
                return;
            }

            var manifestAsset = Resources.Load<TextAsset>("GotchiManifest");
            if (manifestAsset != null)
            {
                try
                {
                    var manifest = JsonUtility.FromJson<Manifest>(manifestAsset.text);
                    if (manifest?.rows != null)
                        foreach (var r in manifest.rows)
                            if (!string.IsNullOrEmpty(r.collateral)) rowByCollateral[r.collateral] = r.row;
                    if (manifest?.defaultSlots != null && manifest.defaultSlots.Length > 0)
                        defaultSlots = manifest.defaultSlots;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[GotchiArt] manifest unreadable: " + e.Message);
                }
            }

            tex.filterMode = FilterMode.Point;
            cache = new Sprite[rows, Cols];
            var pivot = new Vector2(0.5f, FeetPixels / Cell);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    // the sheet is authored top down, Unity textures are bottom up
                    var rect = new Rect(col * Cell, tex.height - (row + 1) * Cell, Cell, Cell);
                    cache[row, col] = Sprite.Create(tex, rect, pivot, Config.PPU, 0, SpriteMeshType.FullRect);
                    cache[row, col].name = $"gotchi_{row}_{col}";
                }
            }
            available = true;
        }

        /// Paaint names collateral folders ma* for haunt 1 and am* for haunt 2,
        /// while a bound hero carries the bare symbol, so try both prefixes.
        public static int RowFor(string collateral, int haunt, int slot)
        {
            Load();
            int fallback = SlotRow(slot);
            if (string.IsNullOrWhiteSpace(collateral)) return fallback;

            var id = collateral.Trim().ToLowerInvariant();
            if (rowByCollateral.TryGetValue(id, out var direct)) return direct;
            if (id == "matic") id = "wmatic";

            string first = haunt == 2 ? "am" : "ma";
            string second = haunt == 2 ? "ma" : "am";
            if (rowByCollateral.TryGetValue(first + id, out var a)) return a;
            if (rowByCollateral.TryGetValue(second + id, out var b)) return b;
            return fallback;
        }

        static int SlotRow(int slot)
        {
            if (defaultSlots != null && defaultSlots.Length > 0)
            {
                var name = defaultSlots[Mathf.Clamp(slot, 0, defaultSlots.Length - 1)];
                if (rowByCollateral.TryGetValue(name, out var row)) return row;
            }
            return Mathf.Clamp(slot, 0, cache != null ? cache.GetLength(0) - 1 : 0);
        }

        /// Mirrors Art.Ghost's signature: frame 4 is the death pose.
        public static Sprite Ghost(int slot, Facing face, int frame) =>
            Ghost(null, 0, slot, face, frame);

        public static Sprite Ghost(string collateral, int haunt, int slot, Facing face, int frame)
        {
            Load();
            if (!available) return null;

            int row = Mathf.Clamp(RowFor(collateral, haunt, slot), 0, cache.GetLength(0) - 1);
            if (frame >= 4) return cache[row, ColDead];

            // hands alternate every other frame, which reads as a walk cycle
            bool open = ((frame / 2) & 1) == 0;
            int col = face switch
            {
                Facing.Up => open ? ColUpOpen : ColUpClosed,
                Facing.Side => ColSide,
                _ => open ? ColDownOpen : ColDownClosed,
            };
            return cache[row, col];
        }

        /// Distance from the foot pivot up to the middle of the body, so UI
        /// code can centre a bomber icon on a point. Zero for the drawn ghost,
        /// which is already centred in its cell.
        public static float CenterOffsetUnits
        {
            get
            {
                Load();
                return available ? 12.5f / Config.PPU : 0f;
            }
        }

        /// The one pixel bob that the drawn ghost bakes into its frames.
        public static float BobUnits(int frame) =>
            (frame == 1 || frame == 3) ? 1f / Config.PPU : 0f;
    }
}
