using UnityEngine.SceneManagement;

namespace BomberGhst.Screens
{
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string Cartridge = "Cartridge";
        public const string Match = "Main";

        public static void Go(string scene)
        {
            if (SceneManager.GetActiveScene().name == scene) return;
            SceneManager.LoadScene(scene);
        }

        public static void ToTitle() => Go(Title);
        public static void ToCartridges() => Go(Cartridge);
        public static void ToMatch() => Go(Match);
    }
}
