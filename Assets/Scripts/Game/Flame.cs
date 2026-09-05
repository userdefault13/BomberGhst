using UnityEngine;

namespace BomberGhst
{
    public enum FlameKind { Center, Arm, Tip }

    public class Flame : MonoBehaviour
    {
        FlameKind kind;
        SpriteRenderer sr;
        float age;

        public void Init(FlameKind k)
        {
            kind = k;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = Config.SortFlame;
            Frame(0);
        }

        void Frame(int f)
        {
            switch (kind)
            {
                case FlameKind.Center: sr.sprite = Art.FlameCenter(f); break;
                case FlameKind.Arm: sr.sprite = Art.FlameArm(f); break;
                default: sr.sprite = Art.FlameTip(f); break;
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            float n = age / Config.FlameLife;
            if (n >= 1f) { Destroy(gameObject); return; }
            Frame(Mathf.Clamp(Mathf.FloorToInt(n * 4f), 0, 3));
        }
    }
}
