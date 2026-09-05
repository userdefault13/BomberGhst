using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    /// Keyboard control. Each player gets a set of keys; extra keys can be
    /// bound to the same action (P1 answers to both WASD and the arrows when
    /// nobody else is using the arrows).
    public class HumanBrain : IBrain
    {
        public KeyCode[] Up, Down, Left, Right, Bomb;

        public static HumanBrain Wasd(bool alsoArrows)
        {
            var b = new HumanBrain
            {
                Up = alsoArrows ? new[] { KeyCode.W, KeyCode.UpArrow } : new[] { KeyCode.W },
                Down = alsoArrows ? new[] { KeyCode.S, KeyCode.DownArrow } : new[] { KeyCode.S },
                Left = alsoArrows ? new[] { KeyCode.A, KeyCode.LeftArrow } : new[] { KeyCode.A },
                Right = alsoArrows ? new[] { KeyCode.D, KeyCode.RightArrow } : new[] { KeyCode.D },
                Bomb = alsoArrows
                    ? new[] { KeyCode.Space, KeyCode.LeftShift, KeyCode.Return }
                    : new[] { KeyCode.Space, KeyCode.LeftShift },
            };
            return b;
        }

        public static HumanBrain Arrows()
        {
            return new HumanBrain
            {
                Up = new[] { KeyCode.UpArrow },
                Down = new[] { KeyCode.DownArrow },
                Left = new[] { KeyCode.LeftArrow },
                Right = new[] { KeyCode.RightArrow },
                Bomb = new[] { KeyCode.RightShift, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.RightControl },
            };
        }

        static bool Held(KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        static bool Pressed(KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        public void Think(Bomber self, out Vector2 move, out bool dropBomb)
        {
            float x = (Held(Right) ? 1f : 0f) - (Held(Left) ? 1f : 0f);
            float y = (Held(Up) ? 1f : 0f) - (Held(Down) ? 1f : 0f);
            // Vertical wins ties only if the player was already moving vertically.
            move = new Vector2(x, y);
            if (x != 0f && y != 0f)
            {
                if (self.LastDir.y != 0) move = new Vector2(0f, y);
                else move = new Vector2(x, 0f);
            }
            dropBomb = Pressed(Bomb);
        }
    }

    /// Bot ghost. Reads a danger map off the live bombs, runs a BFS over the
    /// grid and picks one of: run away, bomb something, go get something.
    public class BotBrain : IBrain
    {
        const float Inf = 999f;
        const int W = Arena.W, H = Arena.H;

        readonly float[,] danger = new float[W, H];
        readonly int[,] dist = new int[W, H];
        readonly Vector2Int[,] prev = new Vector2Int[W, H];
        readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        readonly List<Vector2Int> blast = new List<Vector2Int>();

        Vector2Int step;
        bool hasStep;
        bool bombNow;
        float planCooldown;
        float bombCooldown;

        /// 0 = dozy, 1 = sharp. Scales reaction time and how often it dithers.
        public float Skill = 0.8f;

        public void Think(Bomber self, out Vector2 move, out bool dropBomb)
        {
            var arena = GameDirector.I.Arena;
            planCooldown -= Time.deltaTime;
            bombCooldown -= Time.deltaTime;

            Vector2 stepCenter = hasStep ? new Vector2(step.x, step.y) : self.Pos;
            bool arrived = !hasStep || Vector2.Distance(self.Pos, stepCenter) < 0.09f;

            if (arrived || planCooldown <= 0f)
            {
                Plan(arena, self);
                planCooldown = Mathf.Lerp(0.34f, 0.10f, Skill);
            }

            dropBomb = false;
            if (bombNow && bombCooldown <= 0f)
            {
                dropBomb = true;
                bombNow = false;
                bombCooldown = 0.35f;
            }

            if (!hasStep) { move = Vector2.zero; return; }

            Vector2 d = new Vector2(step.x, step.y) - self.Pos;
            if (d.sqrMagnitude < 0.0025f) { move = Vector2.zero; return; }
            move = Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                ? new Vector2(Mathf.Sign(d.x), 0f)
                : new Vector2(0f, Mathf.Sign(d.y));
        }

        // ------------------------------------------------------------- planning

        void Plan(Arena arena, Bomber self)
        {
            CurrentSlot = self.Slot;
            var cur = self.TilePos;
            if (!Arena.InBounds(cur.x, cur.y)) { hasStep = false; return; }

            BuildDanger(arena, null, 0, 0, 0);
            float stepTime = 1f / Mathf.Max(self.Speed, 0.1f);

            // 1. Standing somewhere that is about to be on fire? Leave.
            if (danger[cur.x, cur.y] < Inf)
            {
                if (Flee(arena, self, cur, stepTime)) return;
            }

            // 2. Worth planting a bomb here?
            if (self.ActiveBombs < self.BombLimit && danger[cur.x, cur.y] >= Inf && BombValue(arena, self, cur) > 0)
            {
                BuildDanger(arena, self, cur.x, cur.y, self.Power);
                if (Flee(arena, self, cur, stepTime, planOnly: true))
                {
                    bombNow = true;
                    Flee(arena, self, cur, stepTime);
                    return;
                }
                BuildDanger(arena, null, 0, 0, 0);
            }

            // 3. Otherwise go somewhere useful.
            Bfs(arena, self, cur, avoidDanger: true);
            Vector2Int goal;
            if (Nearest(arena, cur, TileGoal.Pickup, out goal) ||
                Nearest(arena, cur, TileGoal.NearSoft, out goal) ||
                Nearest(arena, cur, TileGoal.NearEnemy, out goal))
            {
                SetStepTowards(cur, goal);
                return;
            }

            Wander(arena, self, cur);
        }

        bool Flee(Arena arena, Bomber self, Vector2Int cur, float stepTime, bool planOnly = false)
        {
            Bfs(arena, self, cur, avoidDanger: false);
            Vector2Int best = cur;
            int bestDist = int.MaxValue;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (dist[x, y] < 0 || danger[x, y] < Inf) continue;
                    if (dist[x, y] >= bestDist) continue;
                    // must be reachable before the blast lands
                    if (!SafeRoute(x, y, stepTime)) continue;
                    bestDist = dist[x, y];
                    best = new Vector2Int(x, y);
                }
            if (bestDist == int.MaxValue) return false;
            if (!planOnly) SetStepTowards(cur, best);
            return true;
        }

        /// Walks the BFS parents back and checks each tile is still safe when
        /// the bot would actually stand on it.
        bool SafeRoute(int gx, int gy, float stepTime)
        {
            var node = new Vector2Int(gx, gy);
            int total = dist[gx, gy];
            while (dist[node.x, node.y] > 0)
            {
                float arrive = dist[node.x, node.y] * stepTime;
                if (danger[node.x, node.y] < arrive + 0.25f) return false;
                node = prev[node.x, node.y];
            }
            return total >= 0;
        }

        void SetStepTowards(Vector2Int cur, Vector2Int goal)
        {
            if (goal == cur || dist[goal.x, goal.y] <= 0) { hasStep = false; return; }
            var node = goal;
            while (dist[node.x, node.y] > 1) node = prev[node.x, node.y];
            step = node;
            hasStep = true;
        }

        void Wander(Arena arena, Bomber self, Vector2Int cur)
        {
            var options = new List<Vector2Int>();
            foreach (var d in Arena.Dirs)
            {
                var n = cur + d;
                if (Walkable(arena, self, n.x, n.y) && danger[n.x, n.y] >= Inf) options.Add(n);
            }
            if (options.Count == 0) { hasStep = false; return; }
            step = options[Random.Range(0, options.Count)];
            hasStep = true;
        }

        // -------------------------------------------------------------- scoring

        enum TileGoal { Pickup, NearSoft, NearEnemy }

        bool Nearest(Arena arena, Vector2Int cur, TileGoal kind, out Vector2Int goal)
        {
            goal = cur;
            int best = int.MaxValue;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (dist[x, y] <= 0 || dist[x, y] >= best) continue;
                    bool match = false;
                    switch (kind)
                    {
                        case TileGoal.Pickup:
                            match = arena.PowerAt[x, y] != null;
                            break;
                        case TileGoal.NearSoft:
                            foreach (var d in Arena.Dirs)
                            {
                                int nx = x + d.x, ny = y + d.y;
                                if (Arena.InBounds(nx, ny) && arena.Tiles[nx, ny] == Tile.Soft) { match = true; break; }
                            }
                            break;
                        case TileGoal.NearEnemy:
                            foreach (var other in GameDirector.I.Bombers)
                            {
                                if (!other.Alive || other.Slot == CurrentSlot) continue;
                                var t = other.TilePos;
                                if (Mathf.Abs(t.x - x) + Mathf.Abs(t.y - y) <= 1) { match = true; break; }
                            }
                            break;
                    }
                    if (!match) continue;
                    best = dist[x, y];
                    goal = new Vector2Int(x, y);
                }
            return best != int.MaxValue;
        }

        int CurrentSlot = -1;

        int BombValue(Arena arena, Bomber self, Vector2Int cur)
        {
            int score = 0;
            foreach (var d in Arena.Dirs)
            {
                for (int s = 1; s <= self.Power; s++)
                {
                    int x = cur.x + d.x * s, y = cur.y + d.y * s;
                    if (!Arena.InBounds(x, y) || arena.Tiles[x, y] == Tile.Hard) break;
                    if (arena.Tiles[x, y] == Tile.Soft) { score += 2; break; }
                    foreach (var other in GameDirector.I.Bombers)
                    {
                        if (!other.Alive || other.Slot == self.Slot) continue;
                        if (other.TilePos == new Vector2Int(x, y)) score += 6;
                    }
                }
            }
            return score;
        }

        // ----------------------------------------------------------- grid utils

        void BuildDanger(Arena arena, Bomber self, int bx, int by, int power)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    danger[x, y] = arena.IsBurning(x, y) ? 0f : Inf;

            foreach (var bomb in arena.Bombs)
            {
                if (bomb == null || bomb.Spent) continue;
                blast.Clear();
                bomb.Blast(blast);
                foreach (var t in blast)
                    danger[t.x, t.y] = Mathf.Min(danger[t.x, t.y], Mathf.Max(bomb.Fuse, 0f));
            }

            if (self != null)
            {
                // hypothetical bomb the bot is considering
                danger[bx, by] = Mathf.Min(danger[bx, by], Config.FuseTime);
                foreach (var d in Arena.Dirs)
                    for (int s = 1; s <= power; s++)
                    {
                        int x = bx + d.x * s, y = by + d.y * s;
                        if (!Arena.InBounds(x, y) || arena.Tiles[x, y] == Tile.Hard) break;
                        danger[x, y] = Mathf.Min(danger[x, y], Config.FuseTime);
                        if (arena.Tiles[x, y] == Tile.Soft) break;
                    }
            }
        }

        bool Walkable(Arena arena, Bomber self, int x, int y)
        {
            if (!Arena.InBounds(x, y) || arena.Tiles[x, y] != Tile.Empty) return false;
            var b = arena.BombAt[x, y];
            return b == null || !b.Blocks(self);
        }

        void Bfs(Arena arena, Bomber self, Vector2Int start, bool avoidDanger)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++) dist[x, y] = -1;

            queue.Clear();
            dist[start.x, start.y] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Arena.Dirs)
                {
                    int x = c.x + d.x, y = c.y + d.y;
                    if (!Walkable(arena, self, x, y) || dist[x, y] >= 0) continue;
                    if (avoidDanger && danger[x, y] < 1.2f) continue;
                    dist[x, y] = dist[c.x, c.y] + 1;
                    prev[x, y] = c;
                    queue.Enqueue(new Vector2Int(x, y));
                }
            }
        }
    }
}
