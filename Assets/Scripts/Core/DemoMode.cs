using UnityEngine;

namespace BomberGhst
{
    /// Command line helpers, used for the attract mode and for grabbing
    /// screenshots of a real build without a human at the keyboard.
    ///   -autoplay              every bomber is a bot, match starts itself
    ///   -mute                  silence music and effects
    ///   -shot &lt;path&gt;    write a PNG every -shotevery seconds
    ///   -shotevery &lt;sec&gt;
    ///   -exitafter &lt;sec&gt;
    ///   -roundtime &lt;sec&gt;   shorten the round clock
    ///   -cartridgetest &lt;id&gt; read one cartridge off chain, log it, quit
    ///   -scene &lt;name&gt;      boot straight into Title, Cartridge or Main
    ///   -devwallet [addr]    connect a dev wallet at boot, no browser needed
    ///   -source &lt;name&gt;     sim, chain or fixture
    public class DemoMode : MonoBehaviour
    {
        public static bool AutoPlay { get; private set; }
        public static bool Mute { get; private set; }
        /// Cartridge id to read and dump at boot, for verifying the chain layer
        /// without a browser. Null when the flag was not passed.
        public static string CartridgeTestId { get; private set; }
        /// Scene to jump to at boot, for looking at one screen directly.
        public static string StartScene { get; private set; }
        /// Fixture wallet to connect at boot when there is no browser wallet.
        public static string DevWallet { get; private set; }
        /// Cartridge back end to select at boot: sim, chain or fixture.
        public static string SourceName { get; private set; }
        /// Overrides the round clock, handy for exercising sudden death.
        public static float RoundTime { get; private set; } = Config.RoundTime;

        string shotPath;
        float shotEvery = 4f;
        float exitAfter = -1f;
        float nextShot;
        int shotIndex;

        void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-autoplay": AutoPlay = true; break;
                    case "-mute": Mute = true; break;
                    case "-scene":
                        if (i + 1 < args.Length) StartScene = args[++i];
                        break;
                    case "-source":
                        if (i + 1 < args.Length) SourceName = args[++i];
                        break;
                    case "-devwallet":
                        DevWallet = i + 1 < args.Length && !args[i + 1].StartsWith("-")
                            ? args[++i]
                            : "0x2127AA7265D573Aa467f1D73554D17890b872E76";
                        break;
                    case "-cartridgetest":
                        CartridgeTestId = i + 1 < args.Length ? args[++i] : "1";
                        break;
                    case "-shot": if (i + 1 < args.Length) shotPath = args[++i]; break;
                    case "-shotevery": if (i + 1 < args.Length) float.TryParse(args[++i], out shotEvery); break;
                    case "-exitafter": if (i + 1 < args.Length) float.TryParse(args[++i], out exitAfter); break;
                    case "-roundtime":
                        if (i + 1 < args.Length && float.TryParse(args[++i], out float rt)) RoundTime = rt;
                        break;
                }
            }
            nextShot = shotEvery;
        }


        void Start()
        {
            if (!string.IsNullOrEmpty(CartridgeTestId))
            {
                StartCoroutine(RunCartridgeTest());
                return;
            }

            if (!string.IsNullOrEmpty(SourceName) || !string.IsNullOrEmpty(DevWallet))
            {
                var svc = Cartridges.CartridgeService.Ensure();
                if (!string.IsNullOrEmpty(SourceName) &&
                    System.Enum.TryParse<Cartridges.CartridgeSource>(SourceName, true, out var source))
                    svc.SetSource(source);
                if (!string.IsNullOrEmpty(DevWallet)) svc.Connect(DevWallet);
            }

            if (!string.IsNullOrEmpty(StartScene))
                UnityEngine.SceneManagement.SceneManager.LoadScene(StartScene);
        }

        System.Collections.IEnumerator RunCartridgeTest()
        {
            const string devOwner = "0x2127AA7265D573Aa467f1D73554D17890b872E76";

            Debug.Log($"[chaintest] sim root={Cartridges.SimApi.Root}");
            var sim = new Cartridges.SimCartridgeProvider(devOwner, null);
            yield return sim.LoadForPlayer(devOwner,
                cart => Debug.Log($"[chaintest] sim playerCartridge -> exists={cart.exists} " +
                                  $"id={cart.cartridgeId} lineA={cart.lineAPaid} hero={cart.heroLabel ?? "none"}"),
                error => Debug.LogError("[chaintest] sim read failed: " + error));

            var provider = new Cartridges.ChainCartridgeProvider(null);
            Debug.Log($"[chaintest] chain={Chain.ChainConfig.Network} rpc={Chain.ChainConfig.Rpc}");
            Debug.Log($"[chaintest] gameId={Chain.ChainConfig.GameId} hash={Chain.ChainConfig.GameIdHash}");

            yield return provider.LoadById(CartridgeTestId,
                cart => Debug.Log(
                    $"[chaintest] id={cart.cartridgeId} exists={cart.exists} owner={cart.owner} " +
                    $"game={cart.gameIdHash} lineA={cart.lineAPaid} hero={cart.activeHeroId} " +
                    $"heroes={cart.heroIds.Count} pocket={cart.pocketGhstWei} " +
                    $"cp=#{cart.checkpoint.nonce}/{cart.checkpoint.stateHash}/'{cart.checkpoint.stateUri}'"),
                error => Debug.LogError("[chaintest] read failed: " + error));

            yield return provider.LoadForPlayer("0x8842E0986F9974209593C8dCB5Aa78226435873a",
                cart => Debug.Log($"[chaintest] playerCartridge -> exists={cart.exists} id={cart.cartridgeId}"),
                error => Debug.LogError("[chaintest] player read failed: " + error));

            Debug.Log("[chaintest] done");
            Application.Quit();
        }

        void Update()
        {
            if (shotPath != null && Time.time >= nextShot)
            {
                nextShot += shotEvery;
                ScreenCapture.CaptureScreenshot($"{shotPath}/shot_{shotIndex++:00}.png");
            }
            if (exitAfter > 0f && Time.time >= exitAfter) Application.Quit();
        }
    }
}
