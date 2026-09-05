using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BomberGhst.EditorTools
{
    /// One-shot project configuration. The game builds itself at runtime, so
    /// the scene is deliberately empty; this just makes sure it exists, is in
    /// the build, and that the player window opens at an integer scale of the
    /// 320x224 Neo Geo frame.
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string TitleScenePath = "Assets/Scenes/Title.unity";
        const string CartridgeScenePath = "Assets/Scenes/Cartridge.unity";

        [MenuItem("BomberGhst/Set Up Project")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Every scene is intentionally empty: Boot reads the scene name and
            // spawns the matching screen. Title boots first.
            foreach (var path in new[] { TitleScenePath, CartridgeScenePath, ScenePath })
            {
                if (System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(CartridgeScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };

            PlayerSettings.companyName = "BomberGhst";
            PlayerSettings.productName = "BomberGhst";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = Config.ViewW * 3;
            PlayerSettings.defaultScreenHeight = Config.ViewH * 3;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, true);

            QualitySettings.vSyncCount = 1;

            AssetDatabase.SaveAssets();
            Debug.Log("BomberGhst: project set up.");
        }

        /// Batch entry point for CI / scripted builds: makes a macOS player in
        /// Builds/ and fails the process if the build did not succeed.
        public static void BuildMac()
        {
            Run();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { TitleScenePath, CartridgeScenePath, ScenePath },
                locationPathName = "Builds/BomberGhst.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("BomberGhst build: " + report.summary.result);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// WebGL is the shipping target: the game runs inside the Aarcade
        /// GameViewer, which is where the session bridge and the wallet live.
        public static void BuildWebGL()
        {
            Run();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { TitleScenePath, CartridgeScenePath, ScenePath },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("BomberGhst webgl build: " + report.summary.result);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// Batch entry point: sets up, then reports compile status via exit code.
        public static void RunHeadless()
        {
            Run();
            EditorApplication.Exit(0);
        }
    }
}
