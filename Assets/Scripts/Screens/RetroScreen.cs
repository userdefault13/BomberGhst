using UnityEngine;

namespace BomberGhst.Screens
{
    /// Shared plumbing for the front end screens: the same 320x224 pixel frame
    /// the match uses, plus helpers that place things in screen pixels.
    public abstract class RetroScreen : MonoBehaviour
    {
        protected Camera Cam;

        protected virtual void Awake()
        {
            Application.targetFrameRate = 60;
            BuildCamera();
            var bg = Sprite("Backdrop", Art.Backdrop(), 0f, 0f, -500);
            bg.color = Color.white;
            Build();
        }

        protected abstract void Build();

        void BuildCamera()
        {
            var go = new GameObject("PixelCamera") { tag = "MainCamera" };
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            Cam = go.AddComponent<Camera>();
            Cam.orthographic = true;
            Cam.orthographicSize = Config.OrthoSize;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Pal.Ink;
            Cam.nearClipPlane = -1f;
            Cam.farClipPlane = 50f;
            go.AddComponent<Letterbox>();
            go.AddComponent<AudioListener>();
        }

        /// Screen pixels, origin at the centre of the frame, y up.
        protected static Vector3 P(float px, float py) =>
            new Vector3(px / Config.PPU, py / Config.PPU, 0f);

        protected SpriteRenderer Sprite(string name, Sprite sprite, float px, float py, int order,
            Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = P(px, py);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        protected Label Text(string name, float px, float py, float scale = 1f, int order = 10,
            Transform parent = null)
        {
            var lb = Label.Create(parent != null ? parent : transform, name, order, scale);
            lb.transform.position = P(px, py);
            return lb;
        }

        /// A left aligned label: Label sprites are centred, so this shifts by
        /// half the rendered width once the text is known.
        protected Label TextLeft(string name, float px, float py, float scale = 1f, int order = 10)
        {
            var lb = Text(name, px, py, scale, order);
            lb.gameObject.AddComponent<LeftAlign>().Init(px, py, scale);
            return lb;
        }

        protected static bool Pressed(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }
    }

    /// Keeps a centred Label pinned by its left edge instead.
    public class LeftAlign : MonoBehaviour
    {
        float px, py, scale;
        SpriteRenderer sr;
        float lastWidth = -1f;

        public void Init(float x, float y, float s)
        {
            px = x; py = y; scale = s;
            sr = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (sr == null || sr.sprite == null) return;
            float w = sr.sprite.rect.width * scale;
            if (Mathf.Approximately(w, lastWidth)) return;
            lastWidth = w;
            transform.position = new Vector3((px + w * 0.5f) / Config.PPU, py / Config.PPU, 0f);
        }
    }
}
