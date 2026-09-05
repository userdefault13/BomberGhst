using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    public enum Facing { Down = 0, Up = 1, Side = 2 }

    /// Every sprite the game uses, drawn procedurally on first request and cached.
    public static class Art
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static Sprite Get(string key, System.Func<Sprite> build)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = build();
                Cache[key] = s;
            }
            return s;
        }

        public static void Clear() => Cache.Clear();

        // ---------------------------------------------------------------- floor

        public static Sprite Floor()
        {
            return Get("floor", () =>
            {
                int w = Config.W * Config.PPU, h = Config.H * Config.PPU;
                var p = new Pix(w, h);
                var rng = new System.Random(1337);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int tx = x / Config.PPU, ty = y / Config.PPU;
                        bool alt = ((tx + ty) & 1) == 0;
                        var c = alt ? Pal.GrassA : Pal.GrassB;
                        // faint 4px weave so large flats do not read as dead space
                        if (((x >> 2) + (y >> 2) & 1) == 0) c = Mul(c, 1.06f);
                        p.Put(x, y, c);
                    }
                for (int i = 0; i < 900; i++)
                {
                    int x = rng.Next(w), y = rng.Next(h);
                    p.Put(x, y, Mul(p.Get(x, y), rng.Next(2) == 0 ? 1.14f : 0.9f));
                }
                // tile seams
                for (int ty = 0; ty <= Config.H; ty++)
                    for (int x = 0; x < w; x++) p.Put(x, ty * Config.PPU, Mul(p.Get(x, ty * Config.PPU), 0.88f));
                for (int tx = 0; tx <= Config.W; tx++)
                    for (int y = 0; y < h; y++) p.Put(tx * Config.PPU, y, Mul(p.Get(tx * Config.PPU, y), 0.88f));
                return p.ToSprite();
            });
        }

        static Color32 Mul(Color32 c, float f)
        {
            return new Color32(
                (byte)Mathf.Clamp(c.r * f, 0, 255),
                (byte)Mathf.Clamp(c.g * f, 0, 255),
                (byte)Mathf.Clamp(c.b * f, 0, 255), c.a);
        }

        // --------------------------------------------------------------- blocks

        public static Sprite HardBlock()
        {
            return Get("hard", () =>
            {
                var p = new Pix(16, 16);
                p.Rect(0, 0, 16, 16, Pal.Ink);
                p.Rect(1, 1, 14, 14, Pal.Steel);
                p.Rect(1, 13, 14, 2, Pal.SteelHi);     // top light
                p.Rect(1, 1, 14, 2, Pal.SteelLo);      // bottom shade
                p.Rect(1, 1, 2, 14, Pal.SteelHi);
                p.Rect(13, 1, 2, 14, Pal.SteelLo);
                p.Rect(3, 3, 10, 10, Pal.Steel);
                p.Frame(3, 3, 10, 10, Pal.SteelLo);
                p.Rect(4, 4, 8, 8, Mul(Pal.Steel, 1.1f));
                foreach (var pt in new[] { new Vector2Int(5, 5), new Vector2Int(10, 5), new Vector2Int(5, 10), new Vector2Int(10, 10) })
                {
                    p.Rect(pt.x, pt.y, 2, 2, Pal.SteelHi);
                    p.Put(pt.x + 1, pt.y, Pal.SteelLo);
                }
                return p.ToSprite();
            });
        }

        public static Sprite SoftBlock(int crumble = 0)
        {
            return Get("soft" + crumble, () =>
            {
                var p = new Pix(16, 16);
                p.Rect(0, 0, 16, 16, Pal.Ink);
                p.Rect(1, 1, 14, 14, Pal.Brick);
                p.Rect(1, 13, 14, 2, Pal.BrickHi);
                p.Rect(1, 1, 14, 2, Pal.BrickLo);
                p.Rect(1, 1, 2, 14, Mul(Pal.Brick, 1.08f));
                p.Rect(13, 1, 2, 14, Pal.BrickLo);
                // mortar courses
                p.Rect(1, 8, 14, 1, Pal.BrickLo);
                p.Rect(7, 9, 1, 6, Pal.BrickLo);
                p.Rect(4, 1, 1, 7, Pal.BrickLo);
                p.Rect(11, 1, 1, 7, Pal.BrickLo);
                p.Put(3, 11, Pal.BrickHi); p.Put(10, 4, Pal.BrickHi);
                if (crumble > 0)
                {
                    p.Dissolve(crumble * 0.22f, 77 + crumble * 13);
                    var scaled = new Pix(16, 16);
                    float k = 1f - crumble * 0.12f;
                    for (int y = 0; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                        {
                            int sx = Mathf.RoundToInt((x - 8) / k + 8), sy = Mathf.RoundToInt((y - 8) / k + 8);
                            scaled.Put(x, y, p.Get(sx, sy));
                        }
                    return scaled.ToSprite();
                }
                return p.ToSprite();
            });
        }

        /// Blocks that rain down during sudden death.
        public static Sprite FallBlock()
        {
            return Get("fall", () =>
            {
                var p = new Pix(16, 16);
                p.Rect(0, 0, 16, 16, Pal.Ink);
                p.Rect(1, 1, 14, 14, Pal.Purple);
                p.Rect(1, 13, 14, 2, Mul(Pal.Purple, 1.35f));
                p.Rect(1, 1, 14, 2, Mul(Pal.Purple, 0.6f));
                p.Frame(4, 4, 8, 8, Mul(Pal.Purple, 0.6f));
                p.Rect(6, 6, 4, 4, Pal.Bone);
                return p.ToSprite();
            });
        }

        // ----------------------------------------------------------------- bomb

        public static Sprite Bomb(int frame)
        {
            return Get("bomb" + frame, () =>
            {
                var p = new Pix(16, 16);
                var shell = Pal.Rgb(0x2A2740);
                p.Disc(8f, 6.8f, 5.6f, shell);
                p.Rect(6, 11, 4, 2, shell);                 // fuse collar
                p.Disc(5.6f, 8.4f, 2.0f, Pal.Rgb(0x4A4670)); // sheen
                p.Rect(5, 9, 2, 2, Pal.Bone);
                p.Disc(8f, 3.2f, 3.4f, Pal.Rgb(0x1B1930));   // grounded shadow side
                p.Disc(8f, 6.8f, 5.6f, shell);
                p.Disc(5.6f, 8.6f, 1.8f, Pal.Rgb(0x4A4670));
                p.Rect(5, 9, 2, 2, Pal.Bone);
                p.Rect(6, 11, 4, 2, shell);
                // fuse
                p.Put(10, 12, Pal.Gray); p.Put(11, 13, Pal.Gray); p.Put(11, 14, Pal.Gray);
                int s = frame == 0 ? 1 : 2;
                p.Rect(11 - s / 2, 14, 1 + s, 1 + s, frame == 0 ? Pal.Yellow : Pal.Orange);
                p.Put(11, 14, Pal.White);
                p.Outline(Pal.Ink);
                return p.ToSprite();
            });
        }

        // ---------------------------------------------------------------- flame

        static readonly float[] FlameW = { 0.55f, 1f, 0.85f, 0.5f };

        static void FlameBands(Pix p, int frame, System.Action<float, Color32> band)
        {
            float k = FlameW[Mathf.Clamp(frame, 0, 3)];
            band(7f * k, Pal.Red);
            band(5.6f * k, Pal.Orange);
            band(3.6f * k, Pal.Yellow);
            band(1.8f * k, Pal.White);
        }

        public static Sprite FlameCenter(int frame)
        {
            return Get("fc" + frame, () =>
            {
                var p = new Pix(16, 16);
                FlameBands(p, frame, (half, c) =>
                {
                    p.Rect(0, Mathf.RoundToInt(8 - half), 16, Mathf.Max(1, Mathf.RoundToInt(half * 2)), c);
                    p.Rect(Mathf.RoundToInt(8 - half), 0, Mathf.Max(1, Mathf.RoundToInt(half * 2)), 16, c);
                    p.Disc(8f, 8f, half * 1.25f, c);
                });
                return p.ToSprite();
            });
        }

        public static Sprite FlameArm(int frame)
        {
            return Get("fa" + frame, () =>
            {
                var p = new Pix(16, 16);
                FlameBands(p, frame, (half, c) =>
                    p.Rect(0, Mathf.RoundToInt(8 - half), 16, Mathf.Max(1, Mathf.RoundToInt(half * 2)), c));
                return p.ToSprite();
            });
        }

        public static Sprite FlameTip(int frame)
        {
            return Get("ft" + frame, () =>
            {
                var p = new Pix(16, 16);
                FlameBands(p, frame, (half, c) =>
                {
                    p.Rect(0, Mathf.RoundToInt(8 - half), 11, Mathf.Max(1, Mathf.RoundToInt(half * 2)), c);
                    p.Disc(10.5f, 8f, half, c);
                    // tapered lick at the end
                    p.Rect(11, Mathf.RoundToInt(8 - half * 0.55f), 3, Mathf.Max(1, Mathf.RoundToInt(half * 1.1f)), c);
                });
                return p.ToSprite();
            });
        }

        // -------------------------------------------------------------- bombers

        /// A little sheet-ghost bomber. 4 walk frames per facing, per team colour.
        public static Sprite Ghost(int team, Facing face, int frame)
        {
            return Get($"g{team}{(int)face}{frame}", () =>
            {
                team = Mathf.Clamp(team, 0, Pal.Team.Length - 1);
                Color32 body = Pal.Team[team], dark = Pal.TeamDark[team], light = Pal.TeamLight[team];
                var p = new Pix(16, 16);
                int bob = (frame == 1 || frame == 3) ? 1 : 0;

                // dome + torso
                p.Disc(8f, 9.6f + bob, 5.6f, body);
                p.Rect(2, 4 + bob, 12, 6, body);
                // scalloped hem, phase shifts with the walk cycle
                for (int x = 2; x < 14; x++)
                {
                    int d = (((x + frame * 2) / 3) & 1) == 0 ? 1 : 3;
                    p.Rect(x, d + bob, 1, 5, body);
                }
                // shading
                p.Disc(10.4f, 8.4f + bob, 4.4f, body);
                p.Rect(11, 1 + bob, 3, 9, Mul(body, 0.86f));
                p.Disc(5.4f, 11.4f + bob, 2.6f, light);   // top-left sheen

                bool dead = frame == 4;
                DrawFace(p, face, bob, dead);

                // hem shadow
                for (int x = 2; x < 14; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        if (p.Get(x, y).a != 0) { p.Put(x, y, y <= 1 + bob ? (Color32)dark : p.Get(x, y)); break; }
                    }
                }
                p.Outline(Pal.Ink);
                return p.ToSprite();
            });
        }

        static void DrawFace(Pix p, Facing face, int bob, bool dead)
        {
            int ey = 10 + bob;
            int lx = 5, rx = 11;
            if (dead)
            {
                foreach (int cx in new[] { lx, rx })
                {
                    p.Oval(cx, ey, 2.2f, 2.6f, Pal.Bone);
                    for (int i = -1; i <= 1; i++)
                    {
                        p.Put(cx + i, ey + i, Pal.Ink);
                        p.Put(cx + i, ey - i, Pal.Ink);
                    }
                }
                p.Oval(8, ey - 4, 1.6f, 1.2f, Pal.Ink);
                return;
            }

            p.Oval(lx, ey, 2.2f, 2.7f, Pal.Bone);
            p.Oval(rx, ey, 2.2f, 2.7f, Pal.Bone);

            int px = 0, py = 0;
            switch (face)
            {
                case Facing.Down: py = -1; break;
                case Facing.Up: py = 1; break;
                case Facing.Side: px = 1; break;
            }
            p.Rect(lx - 1 + px, ey - 1 + py, 2, 3, Pal.Ink);
            p.Rect(rx - 1 + px, ey - 1 + py, 2, 3, Pal.Ink);
            p.Put(lx + px, ey + 1 + py, Pal.White);
            p.Put(rx + px, ey + 1 + py, Pal.White);

            if (face == Facing.Down) p.Oval(8, ey - 4, 1.4f, 1.0f, Pal.Ink);
        }

        // ------------------------------------------------------------- powerups

        static readonly Dictionary<char, Color32> IconMap = new Dictionary<char, Color32>
        {
            { '1', Pal.Red }, { '2', Pal.Orange }, { '3', Pal.Yellow },
            { '4', Pal.Bone }, { '5', Pal.Cyan }, { '6', Pal.Rgb(0x2A2740) },
            { '7', Pal.White },
        };

        static readonly string[] IcoFire = {
            "...11...",
            "..1221..",
            "..1221..",
            ".122321.",
            "1223321.",
            "1233321.",
            ".122321.",
            "..1111..",
        };
        static readonly string[] IcoBomb = {
            ".....3..",
            "....3...",
            "..666...",
            ".66666..",
            ".67666..",
            ".66666..",
            "..666...",
            "........",
        };
        static readonly string[] IcoSpeed = {
            "....55..",
            "...55...",
            "..55....",
            ".55555..",
            "...55...",
            "..55....",
            ".55.....",
            "........",
        };
        static readonly string[] IcoKick = {
            "..44....",
            "..44....",
            "..44....",
            "..44....",
            "..4444..",
            "..44444.",
            ".444444.",
            "........",
        };

        public static Sprite PowerUp(PowerType type)
        {
            return Get("pu" + (int)type, () =>
            {
                Color32 tint;
                string[] icon;
                switch (type)
                {
                    case PowerType.Fire: tint = Pal.Red; icon = IcoFire; break;
                    case PowerType.Bomb: tint = Pal.Blue; icon = IcoBomb; break;
                    case PowerType.Speed: tint = Pal.Green; icon = IcoSpeed; break;
                    default: tint = Pal.Purple; icon = IcoKick; break;
                }
                var p = new Pix(16, 16);
                p.Rect(2, 2, 12, 12, Mul(tint, 0.45f));
                p.Rect(3, 3, 10, 10, Mul(tint, 0.8f));
                p.Rect(3, 11, 10, 2, Mul(tint, 1.25f));
                p.Rect(3, 3, 10, 1, Mul(tint, 0.5f));
                p.Frame(2, 2, 12, 12, Pal.Ink);
                p.Put(2, 2, Pal.Clear); p.Put(13, 2, Pal.Clear);
                p.Put(2, 13, Pal.Clear); p.Put(13, 13, Pal.Clear);
                p.Stamp(4, 4, icon, IconMap);
                return p.ToSprite();
            });
        }

        // ------------------------------------------------------------------ ui

        public static Sprite Panel(bool left)
        {
            return Get("panel" + left, () =>
            {
                var p = new Pix(Config.PanelW, Config.ViewH);
                p.Fill(Pal.Panel);
                for (int y = 0; y < p.H; y += 4)
                    for (int x = 0; x < p.W; x++)
                        if (((x + y) & 3) == 0) p.Put(x, y, Pal.PanelHi);
                p.Rect(0, 0, p.W, 2, Pal.Ink);
                p.Rect(0, p.H - 2, p.W, 2, Pal.Ink);
                int edge = left ? p.W - 2 : 0;
                p.Rect(edge, 0, 2, p.H, Pal.Ink);
                p.Rect(left ? 0 : p.W - 2, 0, 2, p.H, Pal.PanelHi);
                return p.ToSprite();
            });
        }

        public static Sprite Bar()
        {
            return Get("bar", () =>
            {
                var p = new Pix(Config.ViewW, Config.BarH);
                p.Fill(Pal.Panel);
                p.Rect(0, 0, p.W, 1, Pal.Ink);
                p.Rect(0, p.H - 2, p.W, 2, Pal.PanelHi);
                return p.ToSprite();
            });
        }


        // ------------------------------------------------------------ shell ui

        /// Dark backdrop for the front end screens.
        public static Sprite Backdrop()
        {
            return Get("backdrop", () =>
            {
                var p = new Pix(Config.ViewW, Config.ViewH);
                p.Fill(Pal.Night);
                for (int y = 0; y < p.H; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        if (x % 16 == 0 || y % 16 == 0) p.Put(x, y, Mul(Pal.Night, 1.35f));
                        if (x % 64 == 0 && y % 4 < 2) p.Put(x, y, Mul(Pal.Night, 1.7f));
                    }
                // corner vignette so the middle reads brighter than the edges
                for (int y = 0; y < p.H; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        float dx = (x - p.W * 0.5f) / (p.W * 0.5f);
                        float dy = (y - p.H * 0.5f) / (p.H * 0.5f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > 0.75f) p.Put(x, y, Mul(p.Get(x, y), Mathf.Lerp(1f, 0.45f, (d - 0.75f) / 0.7f)));
                    }
                return p.ToSprite();
            });
        }

        /// A cartridge, Neo Geo shaped: grip ridges up top, label in the middle,
        /// contact strip along the bottom edge.
        public static Sprite CartridgeShell(int tint, bool bound)
        {
            return Get($"cart{tint}{bound}", () =>
            {
                var body = Pal.Team[Mathf.Clamp(tint, 0, Pal.Team.Length - 1)];
                var p = new Pix(40, 48);

                p.Rect(2, 2, 36, 44, Mul(body, 0.55f));
                p.Rect(3, 3, 34, 42, Mul(body, 0.8f));
                p.Rect(3, 43, 34, 2, Mul(body, 1.2f));
                p.Rect(3, 3, 34, 2, Mul(body, 0.4f));

                // grip ridges
                for (int i = 0; i < 5; i++) p.Rect(7 + i * 5, 38, 3, 5, Mul(body, 0.45f));

                // label
                p.Rect(6, 14, 28, 22, Pal.Bone);
                p.Frame(6, 14, 28, 22, Pal.Ink);
                p.Rect(7, 30, 26, 5, Mul(body, 1.05f));

                // contact strip
                p.Rect(8, 3, 24, 7, Pal.Rgb(0x2A2740));
                for (int i = 0; i < 8; i++) p.Rect(9 + i * 3, 4, 2, 5, Pal.Yellow);

                // a little ghost on the label when a hero is bound
                if (bound)
                {
                    p.Disc(20f, 24f, 4.5f, body);
                    p.Rect(16, 19, 9, 5, body);
                    for (int x = 16; x < 25; x++) p.Rect(x, ((x / 3) & 1) == 0 ? 18 : 19, 1, 3, body);
                    p.Rect(18, 24, 2, 3, Pal.Ink);
                    p.Rect(22, 24, 2, 3, Pal.Ink);
                }
                else
                {
                    for (int i = 0; i < 3; i++) p.Rect(11, 20 + i * 5, 18, 2, Pal.Gray);
                }

                p.Outline(Pal.Ink);
                return p.ToSprite();
            });
        }

        /// Selection caret for the front end menus.
        public static Sprite Caret()
        {
            return Get("caret", () =>
            {
                var p = new Pix(6, 7);
                for (int y = 0; y < 7; y++)
                {
                    int w = 3 - Mathf.Abs(3 - y);
                    p.Rect(1, y, Mathf.Max(1, w + 1), 1, Pal.Yellow);
                }
                p.Outline(Pal.Ink);
                return p.ToSprite();
            });
        }

        /// A flat 1x1 white pixel, tinted and scaled for bars/overlays.
        public static Sprite Blank()
        {
            return Get("blank", () =>
            {
                var p = new Pix(1, 1);
                p.Fill(Pal.White);
                return p.ToSprite(0.5f, 0.5f, 1);
            });
        }
    }
}
