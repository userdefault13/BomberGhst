using UnityEngine;

namespace BomberGhst
{
    public interface IBrain
    {
        void Think(Bomber self, out Vector2 move, out bool dropBomb);
    }

    /// A player or bot ghost. Movement is free-roaming but grid aware, with the
    /// classic corner-slide assist so you can round a block without pixel work.
    public class Bomber : MonoBehaviour
    {
        public int Slot;                 // 0..3, also the colour index
        public string Name = "P1";
        public IBrain Brain;
        public bool Alive { get; private set; } = true;

        public Vector2 Pos;              // tile space
        public int Power = Config.StartPower;
        public int BombLimit = Config.StartBombs;
        public int SpeedLevel;
        public bool HasKick;
        public int ActiveBombs;
        public int Wins;

        /// Set for the local player when their cartridge has a hero bound.
        public string Collateral;
        public int Haunt = 1;

        Arena arena;
        SpriteRenderer sr;
        Facing facing = Facing.Down;
        bool faceLeft;
        float animTime;
        int animFrame;
        float deathTime;
        Vector2Int lastDir = Vector2Int.down;

        public float Speed => Mathf.Min(Config.BaseSpeed + Config.SpeedStep * SpeedLevel, Config.MaxSpeed);
        public Vector2Int TilePos => new Vector2Int(Mathf.RoundToInt(Pos.x), Mathf.RoundToInt(Pos.y));
        public Vector2Int LastDir => lastDir;

        public void Init(Arena a, int slot, string name, IBrain brain, Vector2Int spawn)
        {
            arena = a; Slot = slot; Name = name; Brain = brain;
            sr = gameObject.AddComponent<SpriteRenderer>();
            ResetForRound(spawn);
        }

        public void ResetForRound(Vector2Int spawn)
        {
            Alive = true;
            Pos = spawn;
            Power = Config.StartPower;
            BombLimit = Config.StartBombs;
            SpeedLevel = 0;
            HasKick = false;
            ActiveBombs = 0;
            deathTime = 0f;
            facing = Facing.Down;
            faceLeft = false;
            sr.enabled = true;
            sr.color = Color.white;
            transform.localScale = Vector3.one;
            Sync();
        }

        void Update()
        {
            if (!Alive) { DeathAnim(); return; }
            if (!GameDirector.I.AcceptsInput) { Animate(Vector2.zero); Sync(); return; }

            Brain.Think(this, out var move, out bool drop);
            Step(move, Time.deltaTime);

            if (drop) TryPlaceBomb();

            var t = TilePos;
            var pu = arena.PowerAt[t.x, t.y];
            if (pu != null && Vector2.Distance(Pos, t) < 0.55f) pu.Collect(this);

            if (arena.IsBurning(t.x, t.y)) Die();

            Animate(move);
            Sync();
        }

        // ------------------------------------------------------------- movement

        void Step(Vector2 move, float dt)
        {
            if (move.sqrMagnitude < 0.01f) return;

            // Four-way only: commit to the dominant axis.
            Vector2Int dir = Mathf.Abs(move.x) >= Mathf.Abs(move.y)
                ? new Vector2Int((int)Mathf.Sign(move.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(move.y));

            lastDir = dir;
            if (dir.x != 0) { facing = Facing.Side; faceLeft = dir.x < 0; }
            else facing = dir.y > 0 ? Facing.Up : Facing.Down;

            TryMove(dir, Speed * dt);
        }

        void TryMove(Vector2Int dir, float dist)
        {
            Vector2 want = Pos + (Vector2)dir * dist;
            if (Free(want, out var blocker)) { Pos = want; return; }

            // Corner assist: if the lane-aligned tile ahead is clear, drift onto it.
            Vector2 aligned = dir.x != 0
                ? new Vector2(want.x, Mathf.Round(Pos.y))
                : new Vector2(Mathf.Round(Pos.x), want.y);
            if ((aligned - Pos).sqrMagnitude > 0.0001f && Free(aligned, out _))
            {
                Pos = Vector2.MoveTowards(Pos, aligned, dist);
                return;
            }

            if (blocker != null && HasKick) blocker.Kick(dir);

            // Ease right up against the wall instead of stopping a step short.
            Vector2 snug = Pos;
            if (dir.x != 0)
            {
                int ix = Mathf.FloorToInt(want.x + dir.x * Config.BodyHalf + 0.5f);
                float limit = dir.x > 0
                    ? ix - 0.5f - Config.BodyHalf - 0.001f
                    : ix + 0.5f + Config.BodyHalf + 0.001f;
                snug.x = dir.x > 0 ? Mathf.Min(want.x, limit) : Mathf.Max(want.x, limit);
            }
            else
            {
                int iy = Mathf.FloorToInt(want.y + dir.y * Config.BodyHalf + 0.5f);
                float limit = dir.y > 0
                    ? iy - 0.5f - Config.BodyHalf - 0.001f
                    : iy + 0.5f + Config.BodyHalf + 0.001f;
                snug.y = dir.y > 0 ? Mathf.Min(want.y, limit) : Mathf.Max(want.y, limit);
            }
            if (Free(snug, out _)) Pos = snug;
        }

        public bool Free(Vector2 p, out Bomb blocker)
        {
            blocker = null;
            const float h = Config.BodyHalf;
            int x0 = Mathf.FloorToInt(p.x - h + 0.5f), x1 = Mathf.FloorToInt(p.x + h + 0.5f);
            int y0 = Mathf.FloorToInt(p.y - h + 0.5f), y1 = Mathf.FloorToInt(p.y + h + 0.5f);
            bool ok = true;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!Arena.InBounds(x, y) || arena.Tiles[x, y] != Tile.Empty) { ok = false; continue; }
                    var b = arena.BombAt[x, y];
                    if (b != null && b.Blocks(this)) { blocker = b; ok = false; }
                }
            return ok;
        }

        // ---------------------------------------------------------------- state

        void TryPlaceBomb()
        {
            if (ActiveBombs >= BombLimit) return;
            var t = TilePos;
            if (arena.BombAt[t.x, t.y] != null) return;
            if (arena.PlaceBomb(this, t.x, t.y) == null) return;
            ActiveBombs++;
            if (Slot == 0) Cartridges.Progress.RecordBomb();
        }

        public void NotifyBombCleared() => ActiveBombs = Mathf.Max(0, ActiveBombs - 1);

        public void Grant(PowerType type)
        {
            switch (type)
            {
                case PowerType.Fire: Power = Mathf.Min(Power + 1, Config.MaxPower); break;
                case PowerType.Bomb: BombLimit = Mathf.Min(BombLimit + 1, Config.MaxBombs); break;
                case PowerType.Speed: SpeedLevel = Mathf.Min(SpeedLevel + 1, 6); break;
                case PowerType.Kick: HasKick = true; break;
            }
        }

        public void Die()
        {
            if (!Alive) return;
            Alive = false;
            deathTime = Time.time;
            sr.sprite = Art.Ghost(Collateral, Haunt, Slot, Facing.Down, 4);
            sr.flipX = false;
            Sfx.Play(Sound.Death);
            GameDirector.I.OnBomberDied(this);
        }

        void DeathAnim()
        {
            float n = (Time.time - deathTime) / 1.1f;
            if (n >= 1f) { sr.enabled = false; return; }
            transform.position = Config.TileToWorld(Pos.x, Pos.y + FootDrop + n * 1.4f);
            transform.localScale = new Vector3(1f - n * 0.3f, 1f + n * 0.2f, 1f);
            sr.color = new Color(1f, 1f, 1f, 1f - n);
            sr.sortingOrder = Config.SortFlame + 1;
        }

        // ------------------------------------------------------------- rendering

        void Animate(Vector2 move)
        {
            bool moving = move.sqrMagnitude > 0.01f;
            if (moving) animTime += Time.deltaTime * (Speed * 1.9f);
            else animTime += Time.deltaTime * 2.2f;
            int frame = moving ? Mathf.FloorToInt(animTime) % 4 : (Mathf.FloorToInt(animTime) % 2) * 2;
            animFrame = frame;
            sr.sprite = Art.Ghost(Collateral, Haunt, Slot, facing, frame);
            sr.flipX = facing == Facing.Side && faceLeft;
        }

        /// Aavegotchi sprites are pivoted at the feet and stand taller than a
        /// tile, so they sit low on the tile with the body overlapping upwards.
        static float FootDrop => GotchiArt.Available ? -0.2f : 0.06f;

        void Sync()
        {
            float bob = GotchiArt.Available ? GotchiArt.BobUnits(animFrame) : 0f;
            transform.position = Config.TileToWorld(Pos.x, Pos.y + FootDrop + bob);
            sr.sortingOrder = Config.SortActor + Config.YSort(Pos.y, 2);
        }
    }
}
