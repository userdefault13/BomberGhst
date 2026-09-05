using System;
using System.Collections;
using BomberGhst.Aarcade;
using BomberGhst.Chain;
using UnityEngine;

namespace BomberGhst.Cartridges
{
    /// Where cartridge state is read and written. The SIM backend is the live
    /// one today; Chain is the Base Sepolia migration target and Fixture is the
    /// offline stand-in.
    public enum CartridgeSource { Sim, Chain, Fixture }

    /// Single owner of the wallet session and the active cartridge. Survives
    /// scene loads so the title screen, the cartridge screen and the match all
    /// see the same state.
    public class CartridgeService : MonoBehaviour
    {
        public static CartridgeService I { get; private set; }

        public string Address { get; private set; }
        public bool IsGuest { get; private set; }
        public bool Busy { get; private set; }
        public string Status { get; private set; } = "";
        public CartridgeSnapshot Active { get; private set; }
        public ICartridgeProvider Provider { get; private set; }

        public bool Connected => !string.IsNullOrEmpty(Address);
        public bool HasCartridge => Active != null && Active.exists;
        public bool ReadyToPlay => IsGuest || (HasCartridge && Active.lineAPaid);

        /// Collateral of the hero bound to the active cartridge, or null when
        /// nothing is bound. Drives which Aavegotchi the local player wears.
        public string HeroCollateral =>
            Active != null && Active.HasHero ? Active.heroCollateral : null;
        public int HeroHaunt => Active?.heroHaunt ?? 1;

        public CartridgeSource Source { get; private set; } = CartridgeSource.Sim;
        public string SessionToken => Aarcade.AarcadeBridge.Instance?.SessionToken;

        public event Action OnChanged;

        public static CartridgeService Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("CartridgeService");
            DontDestroyOnLoad(go);
            I = go.AddComponent<CartridgeService>();
            return I;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Source = (CartridgeSource)PlayerPrefs.GetInt("cartridge.source", (int)CartridgeSource.Sim);

            var bridge = AarcadeBridge.Instance;
            if (bridge != null)
            {
                bridge.OnSessionReady += OnSession;
                if (bridge.CurrentSession != null) OnSession(bridge.CurrentSession);
            }
        }

        void OnDestroy()
        {
            if (AarcadeBridge.Instance != null) AarcadeBridge.Instance.OnSessionReady -= OnSession;
        }

        void Changed() => OnChanged?.Invoke();

        void OnSession(AarcadeBridge.SessionPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.playerId)) return;
            Connect(payload.playerId);
            if (!string.IsNullOrEmpty(payload.cartridgeId) && payload.cartridgeId != "0")
                StartCoroutine(LoadByIdRoutine(payload.cartridgeId));
        }

        // ---------------------------------------------------------- connecting

        public void Connect(string address)
        {
            Address = address;
            IsGuest = false;
            SelectProvider();
            Status = "Connected " + Hex.Shorten(address);
            Changed();
            Refresh();
        }

        public void PlayAsGuest()
        {
            IsGuest = true;
            Status = "Guest session - no cartridge";
            Changed();
        }

        public void Disconnect()
        {
            Address = null;
            IsGuest = false;
            Active = null;
            Status = "";
            Changed();
        }

        /// Kicks off a wallet connect when the shell did not supply a session.
        public void ConnectWallet()
        {
            if (Busy) return;
            StartCoroutine(ConnectWalletRoutine());
        }

        IEnumerator ConnectWalletRoutine()
        {
            Busy = true;
            Status = "Waiting for wallet...";
            Changed();

            if (!WalletBridge.HasProvider)
            {
                // Editor and desktop have no injected provider; connect the dev
                // address so the screens can still be exercised.
                var fixtureOwner = PlayerPrefs.GetString("chain.devAddress",
                    "0x2127AA7265D573Aa467f1D73554D17890b872E76");
                Busy = false;
                AarcadeBridge.Instance?.ApplyLocalSession(fixtureOwner);
                Connect(fixtureOwner);
                Status = "Dev wallet (no browser wallet here)";
                Changed();
                yield break;
            }

            string address = null;
            string error = null;
            yield return WalletBridge.Ensure().RequestAccounts(a => address = a, e => error = e);
            Busy = false;

            if (error != null || string.IsNullOrEmpty(address))
            {
                Status = error ?? "Wallet returned no account.";
                Changed();
                yield break;
            }

            AarcadeBridge.Instance?.ApplyLocalSession(address);
            Connect(address);
        }

        // ----------------------------------------------------------- provider

        public bool UsingFixture => Source == CartridgeSource.Fixture;

        public void UseFixture(bool on) => SetSource(on ? CartridgeSource.Fixture : CartridgeSource.Sim);

        public void SetSource(CartridgeSource source)
        {
            if (Source == source) return;
            Source = source;
            PlayerPrefs.SetInt("cartridge.source", (int)source);
            SelectProvider();
            Active = null;
            Status = "Source: " + SourceName(source);
            Changed();
            Refresh();
        }

        /// One keypress to flip between the SIM and the testnet, which is how
        /// the migration gets exercised.
        public void CycleSource()
        {
            var next = Source switch
            {
                CartridgeSource.Sim => CartridgeSource.Chain,
                CartridgeSource.Chain => CartridgeSource.Fixture,
                _ => CartridgeSource.Sim,
            };
            SetSource(next);
        }

        public static string SourceName(CartridgeSource source) => source switch
        {
            CartridgeSource.Sim => "SIM",
            CartridgeSource.Chain => "CHAIN",
            _ => "FIXTURE",
        };

        void SelectProvider()
        {
            switch (Source)
            {
                case CartridgeSource.Sim:
                    Provider = new SimCartridgeProvider(Address, SessionToken);
                    break;
                case CartridgeSource.Chain:
                    Provider = ChainConfig.IsConfigured
                        ? (ICartridgeProvider)new ChainCartridgeProvider(Address)
                        : new FixtureCartridgeProvider();
                    break;
                default:
                    Provider = new FixtureCartridgeProvider();
                    break;
            }
        }

        // -------------------------------------------------------------- reads

        public void Refresh()
        {
            if (Busy || string.IsNullOrEmpty(Address)) return;
            StartCoroutine(RefreshRoutine());
        }

        IEnumerator RefreshRoutine()
        {
            Busy = true;
            Status = "Reading " + (Provider?.Label ?? "chain") + "...";
            Changed();

            if (Provider == null) SelectProvider();

            yield return Provider.LoadForPlayer(Address,
                snapshot =>
                {
                    Active = snapshot;
                    Status = snapshot.exists
                        ? "Cartridge #" + snapshot.cartridgeId
                        : "No cartridge for this wallet";
                },
                error => Status = "Read failed: " + Trim(error));

            Busy = false;
            Changed();
        }

        IEnumerator LoadByIdRoutine(string cartridgeId)
        {
            Busy = true; Changed();
            yield return Provider.LoadById(cartridgeId,
                snapshot => Active = snapshot,
                error => Status = "Read failed: " + Trim(error));
            Busy = false; Changed();
        }

        // ------------------------------------------------------------- writes

        public void Mint() => RunWrite("Minting cartridge",
            (p, done, fail) => p.Mint(Address, done, fail));

        public void PayLineA() => RunWrite("Paying line A",
            (p, done, fail) => p.PayLineA(Active, done, fail));

        public void BindStarter() => RunWrite("Binding starter gotchi",
            (p, done, fail) => p.BindStarter(Active, "base-starter", done, fail));

        public void BindOwned(string tokenId) => RunWrite("Binding gotchi #" + tokenId,
            (p, done, fail) => p.BindOwned(Active, tokenId, done, fail));

        public void SaveCheckpoint() => RunWrite("Saving checkpoint",
            (p, done, fail) => p.SaveCheckpoint(Active, Progress.Current, null, done, fail));

        delegate IEnumerator WriteOp(ICartridgeProvider provider, Action<string> done, Action<string> fail);

        void RunWrite(string label, WriteOp op)
        {
            if (Busy) return;
            StartCoroutine(WriteRoutine(label, op));
        }

        IEnumerator WriteRoutine(string label, WriteOp op)
        {
            Busy = true;
            Status = label + "...";
            Changed();

            string failure = null;
            yield return op(Provider, _ => { }, e => failure = e);

            Busy = false;
            Status = failure == null ? label + " done" : Trim(failure);
            Changed();

            if (failure == null) Refresh();
        }

        /// Wallet and RPC errors are long; the panel is 320px wide.
        static string Trim(string message)
        {
            if (string.IsNullOrEmpty(message)) return "failed";
            message = message.Replace("\n", " ").Trim();
            return message.Length <= 46 ? message : message.Substring(0, 45) + "-";
        }
    }
}
