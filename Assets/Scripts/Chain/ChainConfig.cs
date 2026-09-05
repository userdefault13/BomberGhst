using UnityEngine;

namespace BomberGhst.Chain
{
    /// Where the Aarcade cartridge contracts live. Defaults match
    /// config/cartridgeChain.base-sepolia.json in the AarcadeGh-t repo; each
    /// value can be overridden at runtime through PlayerPrefs so a build can be
    /// pointed at another deployment without recompiling.
    public static class ChainConfig
    {
        public const int DefaultChainId = 84532;                 // Base Sepolia
        public const string DefaultNetwork = "base-sepolia";
        public const string DefaultRpc = "https://sepolia.base.org";
        public const string DefaultConsoleDiamond = "0x4c4fa38420c457c5a5a33beebd6f340618e62f8a";
        public const string DefaultCartridgeDiamond = "0xbb44b67de78393a54bcda56707e6ffc719b3a645";
        public const string DefaultGhstToken = "0xe97f36a00058aa7dfc4e85d23532c3f70453a7ae";

        /// The on-chain game id for this title, lowercased the same way
        /// normalizeCartridgeGameId does on the web side.
        public const string GameId = "bomberghst";

        static string Pref(string key, string fallback)
        {
            var v = PlayerPrefs.GetString(key, null);
            return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
        }

        public static int ChainId => PlayerPrefs.GetInt("chain.id", DefaultChainId);
        public static string Network => Pref("chain.network", DefaultNetwork);
        public static string Rpc => Pref("chain.rpc", DefaultRpc);
        public static string ConsoleDiamond => Pref("chain.console", DefaultConsoleDiamond);
        public static string CartridgeDiamond => Pref("chain.cartridge", DefaultCartridgeDiamond);
        public static string GhstToken => Pref("chain.ghst", DefaultGhstToken);

        public static string ChainIdHex => Hex.Quantity(ChainId);

        public static bool IsConfigured =>
            !string.IsNullOrEmpty(ConsoleDiamond) && !string.IsNullOrEmpty(CartridgeDiamond);

        /// bytes32 game id, keccak256 over the lowercase slug.
        public static string GameIdHash => Keccak.HashHex(GameId);
    }
}
