using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    public class Bomb : MonoBehaviour
    {
        public Bomber Owner;
        public int Power;
        public float Fuse;
        public bool Spent;
        public int TileX, TileY;

        Arena arena;
        SpriteRenderer sr;
        float posX, posY;                 // tile space, lags TileX/Y while sliding
        Vector2Int kickDir;
        readonly HashSet<Bomber> passers = new HashSet<Bomber>();

        public void Init(Arena a, Bomber owner, int x, int y)
        {
            arena = a;
            Owner = owner;
            Power = owner != null ? owner.Power : Config.StartPower;
            Fuse = Config.FuseTime;
            TileX = x; TileY = y;
            posX = x; posY = y;

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Bomb(0);
            sr.sortingOrder = Config.SortActor + Config.YSort(y, 1);
            transform.position = Arena.World(x, y);

            // Anyone standing on the bomb right now may walk off it.
            foreach (var b in GameDirector.I.Bombers)
                if (b.Alive && Overlaps(b)) passers.Add(b);
        }

        public bool Blocks(Bomber b) => !passers.Contains(b);

        bool Overlaps(Bomber b)
        {
            const float reach = 0.5f + Config.BodyHalf;
            return Mathf.Abs(b.Pos.x - posX) < reach && Mathf.Abs(b.Pos.y - posY) < reach;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Fuse -= dt;

            passers.RemoveWhere(b => b == null || !b.Alive || !Overlaps(b));

            if (kickDir != Vector2Int.zero) Slide(dt);

            // Tense little pulse that speeds up as the fuse runs out.
            float urgency = Mathf.InverseLerp(Config.FuseTime, 0f, Fuse);
            float rate = Mathf.Lerp(4f, 13f, urgency);
            float pulse = Mathf.Sin(Time.time * rate);
            transform.localScale = new Vector3(1f + pulse * 0.07f, 1f - pulse * 0.07f, 1f);
            sr.sprite = Art.Bomb(pulse > 0f ? 1 : 0);

            if (Fuse <= 0f && !Spent) arena.Detonate(this);
        }

        public void Kick(Vector2Int dir)
        {
            if (kickDir != Vector2Int.zero) return;
            if (!CanEnter(TileX + dir.x, TileY + dir.y)) return;
            kickDir = dir;
            Advance();
            Sfx.Play(Sound.Kick);
        }

        void Slide(float dt)
        {
            float step = Config.KickSpeed * dt;
            var target = new Vector2(TileX, TileY);
            var cur = Vector2.MoveTowards(new Vector2(posX, posY), target, step);
            posX = cur.x; posY = cur.y;
            transform.position = Config.TileToWorld(posX, posY);
            sr.sortingOrder = Config.SortActor + Config.YSort(posY, 1);

            if (cur != target) return;
            if (CanEnter(TileX + kickDir.x, TileY + kickDir.y)) Advance();
            else kickDir = Vector2Int.zero;
        }

        void Advance()
        {
            if (arena.BombAt[TileX, TileY] == this) arena.BombAt[TileX, TileY] = null;
            TileX += kickDir.x; TileY += kickDir.y;
            arena.BombAt[TileX, TileY] = this;
        }

        bool CanEnter(int x, int y)
        {
            if (!Arena.InBounds(x, y) || arena.Tiles[x, y] != Tile.Empty) return false;
            if (arena.BombAt[x, y] != null && arena.BombAt[x, y] != this) return false;
            foreach (var b in GameDirector.I.Bombers)
            {
                if (!b.Alive) continue;
                if (Mathf.Abs(b.Pos.x - x) < 0.7f && Mathf.Abs(b.Pos.y - y) < 0.7f) return false;
            }
            return true;
        }

        /// Tiles this bomb will set on fire, used by the bots to read danger.
        public void Blast(List<Vector2Int> into)
        {
            into.Add(new Vector2Int(TileX, TileY));
            foreach (var d in Arena.Dirs)
                for (int step = 1; step <= Power; step++)
                {
                    int x = TileX + d.x * step, y = TileY + d.y * step;
                    if (!Arena.InBounds(x, y) || arena.Tiles[x, y] == Tile.Hard) break;
                    into.Add(new Vector2Int(x, y));
                    if (arena.Tiles[x, y] == Tile.Soft) break;
                }
        }
    }
}
