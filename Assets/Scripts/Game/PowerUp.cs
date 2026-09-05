using UnityEngine;

namespace BomberGhst
{
    public enum PowerType { Fire, Bomb, Speed, Kick }

    public class PowerUp : MonoBehaviour
    {
        public PowerType Type;
        public int TileX, TileY;

        Arena arena;
        SpriteRenderer sr;
        float birth;

        public void Init(Arena a, PowerType type, int x, int y)
        {
            arena = a; Type = type; TileX = x; TileY = y;
            birth = Time.time;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.PowerUp(type);
            sr.sortingOrder = Config.SortPower;
        }

        void Update()
        {
            float t = Time.time - birth;
            float bob = Mathf.Sin(t * 4f) * 0.055f;
            transform.position = Arena.World(TileX, TileY) + new Vector3(0f, bob, 0f);
            // gentle shimmer so pickups pop against the grass
            float k = 0.82f + 0.18f * Mathf.Sin(t * 7f);
            sr.color = new Color(k, k, k, 1f);
        }

        public void Collect(Bomber by)
        {
            by.Grant(Type);
            Sfx.Play(Sound.Pickup);
            Remove();
        }

        /// Caught in a blast: pickups burn away.
        public void Torch()
        {
            Remove();
        }

        void Remove()
        {
            if (arena != null && arena.PowerAt[TileX, TileY] == this) arena.PowerAt[TileX, TileY] = null;
            Destroy(gameObject);
        }
    }
}
