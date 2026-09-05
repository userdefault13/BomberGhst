using UnityEngine;

namespace BomberGhst
{
    /// Keeps the 320x224 frame pixel-exact by pillar/letterboxing the camera.
    public class Letterbox : MonoBehaviour
    {
        Camera cam;
        int lastW, lastH;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            if (Screen.width == lastW && Screen.height == lastH) return;
            lastW = Screen.width; lastH = Screen.height;

            float target = Config.ViewW / (float)Config.ViewH;
            float actual = Screen.width / (float)Screen.height;
            if (actual > target)
            {
                float w = target / actual;
                cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = actual / target;
                cam.rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }
    }
}
