using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    public enum Tile : byte { Empty, Hard, Soft }

    /// The playfield: tile grid, the objects sitting on it, and the rules for
    /// blowing bits of it up.
    public class Arena : MonoBehaviour
    {
        public const int W = Config.W;
        public const int H = Config.H;

        public Tile[,] Tiles = new Tile[W, H];
        public Bomb[,] BombAt = new Bomb[W, H];
        public PowerUp[,] PowerAt = new PowerUp[W, H];
        public readonly List<Bomb> Bombs = new List<Bomb>();

        readonly PowerType[,] hidden = new PowerType[W, H];
        readonly bool[,] hasHidden = new bool[W, H];
        readonly SpriteRenderer[,] view = new SpriteRenderer[W, H];
        readonly float[,] burnUntil = new float[W, H];

        Transform blockRoot, itemRoot, fxRoot;
        System.Random rng;

        public static readonly Vector2Int[] Dirs = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        void Awake()
        {
            blockRoot = new GameObject("Blocks").transform; blockRoot.SetParent(transform, false);
            itemRoot = new GameObject("Items").transform; itemRoot.SetParent(transform, false);
            fxRoot = new GameObject("Fx").transform; fxRoot.SetParent(transform, false);

            var floor = new GameObject("Floor");
            floor.transform.SetParent(transform, false);
            var fsr = floor.AddComponent<SpriteRenderer>();
            fsr.sprite = Art.Floor();
            fsr.sortingOrder = Config.SortFloor;
        }

        public static bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public bool Blocked(int x, int y) => !InBounds(x, y) || Tiles[x, y] != Tile.Empty;

        public bool IsBurning(int x, int y) => InBounds(x, y) && burnUntil[x, y] > Time.time;

        public static Vector3 World(int x, int y, float z = 0f) => Config.TileToWorld(x, y, z);

        // ------------------------------------------------------------ generation

        public void Build(int seed, IList<Vector2Int> spawns)
        {
            Clear();
            rng = new System.Random(seed);

            var open = new List<Vector2Int>();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool wall = x == 0 || y == 0 || x == W - 1 || y == H - 1 ||
                                (x % 2 == 0 && y % 2 == 0);
                    Tiles[x, y] = wall ? Tile.Hard : Tile.Empty;
                    if (!wall) open.Add(new Vector2Int(x, y));
                }

            // Keep every spawn corner and its elbow clear so nobody starts boxed in.
            var safe = new HashSet<Vector2Int>();
            foreach (var s in spawns)
            {
                safe.Add(s);
                foreach (var d in Dirs) safe.Add(s + d);
                safe.Add(s + new Vector2Int(1, 1));
                safe.Add(s + new Vector2Int(-1, -1));
                safe.Add(s + new Vector2Int(1, -1));
                safe.Add(s + new Vector2Int(-1, 1));
            }

            var softs = new List<Vector2Int>();
            foreach (var c in open)
            {
                if (safe.Contains(c)) continue;
                if (rng.NextDouble() > Config.SoftFill) continue;
                Tiles[c.x, c.y] = Tile.Soft;
                softs.Add(c);
            }

            // Stock the destructible blocks with pickups.
            Shuffle(softs);
            int n = softs.Count;
            int fire = Mathf.RoundToInt(n * 0.16f);
            int bombs = Mathf.RoundToInt(n * 0.14f);
            int speed = Mathf.RoundToInt(n * 0.08f);
            int kick = Mathf.RoundToInt(n * 0.05f);
            int i = 0;
            void Stock(int count, PowerType type)
            {
                for (int k = 0; k < count && i < n; k++, i++)
                {
                    var c = softs[i];
                    hidden[c.x, c.y] = type;
                    hasHidden[c.x, c.y] = true;
                }
            }
            Stock(fire, PowerType.Fire);
            Stock(bombs, PowerType.Bomb);
            Stock(speed, PowerType.Speed);
            Stock(kick, PowerType.Kick);

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (Tiles[x, y] != Tile.Empty) MakeView(x, y);
        }

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var b in Bombs.ToArray()) if (b != null) Destroy(b.gameObject);
            Bombs.Clear();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (view[x, y] != null) Destroy(view[x, y].gameObject);
                    view[x, y] = null;
                    if (PowerAt[x, y] != null) Destroy(PowerAt[x, y].gameObject);
                    PowerAt[x, y] = null;
                    BombAt[x, y] = null;
                    hasHidden[x, y] = false;
                    burnUntil[x, y] = 0f;
                    Tiles[x, y] = Tile.Empty;
                }
            for (int i = fxRoot.childCount - 1; i >= 0; i--) Destroy(fxRoot.GetChild(i).gameObject);
        }

        void MakeView(int x, int y)
        {
            var go = new GameObject($"{Tiles[x, y]}_{x}_{y}");
            go.transform.SetParent(blockRoot, false);
            go.transform.position = World(x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Tiles[x, y] == Tile.Hard ? Art.HardBlock() : Art.SoftBlock();
            sr.sortingOrder = Config.SortBlock + Config.YSort(y);
            view[x, y] = sr;
        }

        // ---------------------------------------------------------------- bombs

        public Bomb PlaceBomb(Bomber owner, int x, int y)
        {
            if (!InBounds(x, y) || BombAt[x, y] != null || Tiles[x, y] != Tile.Empty) return null;
            var go = new GameObject($"Bomb_{x}_{y}");
            go.transform.SetParent(fxRoot, false);
            var bomb = go.AddComponent<Bomb>();
            bomb.Init(this, owner, x, y);
            BombAt[x, y] = bomb;
            Bombs.Add(bomb);
            Sfx.Play(Sound.Place);
            return bomb;
        }

        /// Detonates a bomb and everything it chains into, this frame.
        public void Detonate(Bomb first)
        {
            var queue = new Queue<Bomb>();
            queue.Enqueue(first);
            first.Fuse = 0f;
            bool anyBoom = false;

            while (queue.Count > 0)
            {
                var bomb = queue.Dequeue();
                if (bomb == null || bomb.Spent) continue;
                bomb.Spent = true;
                anyBoom = true;

                int bx = bomb.TileX, by = bomb.TileY;
                if (InBounds(bx, by) && BombAt[bx, by] == bomb) BombAt[bx, by] = null;
                Bombs.Remove(bomb);
                bomb.Owner?.NotifyBombCleared();
                Destroy(bomb.gameObject);

                Burn(bx, by, FlameKind.Center, 0f);

                foreach (var d in Dirs)
                {
                    for (int step = 1; step <= bomb.Power; step++)
                    {
                        int x = bx + d.x * step, y = by + d.y * step;
                        if (!InBounds(x, y) || Tiles[x, y] == Tile.Hard) break;

                        if (Tiles[x, y] == Tile.Soft)
                        {
                            BreakSoft(x, y);
                            break;
                        }

                        var other = BombAt[x, y];
                        if (other != null && !other.Spent) queue.Enqueue(other);

                        bool tip = step == bomb.Power || WillStop(x + d.x, y + d.y);
                        Burn(x, y, tip ? FlameKind.Tip : FlameKind.Arm, Angle(d));
                        if (other != null) break;
                    }
                }
            }

            if (anyBoom) Sfx.Play(Sound.Explode, Random.Range(0.92f, 1.08f));
        }

        bool WillStop(int x, int y) => !InBounds(x, y) || Tiles[x, y] != Tile.Empty;

        static float Angle(Vector2Int d)
        {
            if (d.x > 0) return 0f;
            if (d.x < 0) return 180f;
            if (d.y > 0) return 90f;
            return 270f;
        }

        void Burn(int x, int y, FlameKind kind, float angle)
        {
            burnUntil[x, y] = Mathf.Max(burnUntil[x, y], Time.time + Config.FlameLife);

            var go = new GameObject("Flame");
            go.transform.SetParent(fxRoot, false);
            go.transform.position = World(x, y);
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            go.AddComponent<Flame>().Init(kind);

            var pu = PowerAt[x, y];
            if (pu != null) pu.Torch();
        }

        public void BreakSoft(int x, int y)
        {
            if (!InBounds(x, y) || Tiles[x, y] != Tile.Soft) return;
            Tiles[x, y] = Tile.Empty;
            var sr = view[x, y];
            view[x, y] = null;
            if (sr != null) StartCoroutine(Crumble(sr));

            if (hasHidden[x, y])
            {
                hasHidden[x, y] = false;
                SpawnPower(x, y, hidden[x, y]);
            }
        }

        IEnumerator Crumble(SpriteRenderer sr)
        {
            for (int f = 1; f <= 4; f++)
            {
                sr.sprite = Art.SoftBlock(Mathf.Min(f, 3));
                sr.color = new Color(1f, 1f, 1f, 1f - f * 0.2f);
                yield return new WaitForSeconds(0.05f);
            }
            if (sr != null) Destroy(sr.gameObject);
        }

        public void SpawnPower(int x, int y, PowerType type)
        {
            if (PowerAt[x, y] != null) return;
            var go = new GameObject("Power_" + type);
            go.transform.SetParent(itemRoot, false);
            go.transform.position = World(x, y);
            var pu = go.AddComponent<PowerUp>();
            pu.Init(this, type, x, y);
            PowerAt[x, y] = pu;
        }

        /// Sudden death: a hard block slams down, flattening whatever was there.
        public void DropBlock(int x, int y)
        {
            if (!InBounds(x, y)) return;
            if (Tiles[x, y] == Tile.Soft) BreakSoft(x, y);
            if (PowerAt[x, y] != null) PowerAt[x, y].Torch();
            var bomb = BombAt[x, y];
            if (bomb != null) Detonate(bomb);

            if (view[x, y] != null) Destroy(view[x, y].gameObject);
            Tiles[x, y] = Tile.Hard;
            MakeView(x, y);
            view[x, y].sprite = Art.FallBlock();
            StartCoroutine(SlamIn(view[x, y].transform));
            Sfx.Play(Sound.Drop, Random.Range(0.9f, 1.1f));
        }

        IEnumerator SlamIn(Transform t)
        {
            Vector3 end = t.position;
            Vector3 start = end + Vector3.up * 8f;
            for (float k = 0f; k < 1f; k += Time.deltaTime * 6f)
            {
                t.position = Vector3.Lerp(start, end, k * k);
                yield return null;
            }
            t.position = end;
        }

        /// Spiral of interior tiles, outside-in clockwise, for sudden death.
        public static List<Vector2Int> SpiralOrder()
        {
            var list = new List<Vector2Int>();
            int x0 = 1, y0 = 1, x1 = W - 2, y1 = H - 2;
            while (x0 <= x1 && y0 <= y1)
            {
                for (int x = x0; x <= x1; x++) list.Add(new Vector2Int(x, y1));
                for (int y = y1 - 1; y >= y0; y--) list.Add(new Vector2Int(x1, y));
                if (y0 < y1) for (int x = x1 - 1; x >= x0; x--) list.Add(new Vector2Int(x, y0));
                if (x0 < x1) for (int y = y0 + 1; y <= y1 - 1; y++) list.Add(new Vector2Int(x0, y));
                x0++; y0++; x1--; y1--;
            }
            return list;
        }
    }
}
