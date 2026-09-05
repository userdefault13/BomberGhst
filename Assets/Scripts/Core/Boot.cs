using BomberGhst.Cartridges;
using BomberGhst.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BomberGhst
{
    /// The whole game builds itself from code, so the scene assets are empty and
    /// the scene name alone decides which screen gets spawned into them.
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            EnsureServices();
            SceneManager.sceneLoaded += (scene, mode) => Spawn(scene.name);
            Spawn(SceneManager.GetActiveScene().name);
        }

        /// Everything that has to outlive a scene change.
        static void EnsureServices()
        {
            if (Object.FindAnyObjectByType<DemoMode>() == null)
            {
                var go = new GameObject("DemoMode");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<DemoMode>();
            }
            CartridgeService.Ensure();
        }

        static void Spawn(string sceneName)
        {
            switch (sceneName)
            {
                case SceneFlow.Title: Make<TitleScene>("TitleScreen"); break;
                case SceneFlow.Cartridge: Make<CartridgeScene>("CartridgeScreen"); break;
                default: Make<GameDirector>("BomberGhst"); break;
            }
        }

        static void Make<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindAnyObjectByType<T>() != null) return;
            new GameObject(name).AddComponent<T>();
        }
    }
}
