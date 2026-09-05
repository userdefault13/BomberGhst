using System;
using System.Collections;
using System.Numerics;
using BomberGhst.Chain;
using UnityEngine;

namespace BomberGhst.Cartridges
{
    /// Talks to the Aarcade console and cartridge diamonds directly. Views go
    /// over JSON-RPC so they work everywhere; writes go through the page wallet.
    public class ChainCartridgeProvider : ICartridgeProvider
    {
        const string SigPlayerCartridge = "playerCartridge(bytes32,address)";
        const string SigOwnerOf = "ownerOf(uint256)";
        const string SigGetGameId = "getGameId(uint256)";
        const string SigLineAPaid = "lineAPaid(uint256)";
        const string SigActiveHero = "getActiveHeroId(uint256)";
        const string SigHeroIds = "heroIds(uint256)";
        const string SigGetCheckpoint = "getCheckpoint(uint256)";
        const string SigPocketBalance = "pocketBalance(uint256,bytes32)";
        const string SigMintCartridge = "mintCartridge(bytes32,address)";
        const string SigPayLineA = "payLineA(uint256)";
        const string SigBindOwned = "bindOwned(uint256,uint256)";
        const string SigBindStarter = "bindStarter(uint256,bytes32,address)";
        const string SigCheckpointSave = "checkpointSave(uint256,bytes32,string)";

        /// Fees the cartridge diamond hard codes.
        public static readonly BigInteger StarterBindFeeWei = BigInteger.Pow(10, 18) * 5;

        readonly string signer;

        public ChainCartridgeProvider(string signerAddress)
        {
            signer = signerAddress;
        }

        public string Label => "chain:" + ChainConfig.Network;
        public bool CanWrite => WalletBridge.HasProvider && !string.IsNullOrEmpty(signer);

        static string GhstCurrency => Keccak.HashHex("ghst");

        // ------------------------------------------------------------- reads

        public IEnumerator LoadForPlayer(string owner, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            if (string.IsNullOrEmpty(owner)) { fail?.Invoke("No wallet address."); yield break; }
            if (!ChainConfig.IsConfigured) { fail?.Invoke("Cartridge chain not configured."); yield break; }

            var rpc = JsonRpc.Ensure();
            string error = null;
            string cartridgeId = null;

            yield return rpc.EthCall(
                ChainConfig.ConsoleDiamond,
                Abi.EncodeCall(SigPlayerCartridge,
                    AbiValue.Bytes32(ChainConfig.GameIdHash),
                    AbiValue.Address(owner)),
                result => cartridgeId = new AbiReader(result).Uint(0).ToString(),
                e => error = e);

            if (error != null) { fail?.Invoke(error); yield break; }
            if (string.IsNullOrEmpty(cartridgeId) || cartridgeId == "0")
            {
                done?.Invoke(CartridgeSnapshot.Missing(owner));
                yield break;
            }

            yield return LoadById(cartridgeId, done, fail);
        }

        public IEnumerator LoadById(string cartridgeId, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            if (!BigInteger.TryParse(cartridgeId, out var id) || id.IsZero)
            {
                done?.Invoke(CartridgeSnapshot.Missing(signer));
                yield break;
            }

            var rpc = JsonRpc.Ensure();
            var cart = new CartridgeSnapshot
            {
                cartridgeId = cartridgeId,
                gameId = ChainConfig.GameId,
                gameIdHash = ChainConfig.GameIdHash,
                owner = signer,
            };

            // ownerOf reverts for an unminted id, which is how we detect one
            string ownerErr = null;
            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigOwnerOf, AbiValue.Uint(id)),
                result =>
                {
                    var reader = new AbiReader(result);
                    if (!reader.IsEmpty) { cart.owner = reader.Address(0); cart.exists = true; }
                },
                e => ownerErr = e);

            if (!cart.exists)
            {
                if (ownerErr != null && ownerErr.IndexOf("!minted", StringComparison.OrdinalIgnoreCase) < 0)
                    Debug.LogWarning("[Cartridge] ownerOf: " + ownerErr);
                done?.Invoke(CartridgeSnapshot.Missing(signer));
                yield break;
            }

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigGetGameId, AbiValue.Uint(id)),
                result => cart.gameIdHash = new AbiReader(result).Bytes32(0), Warn);

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigLineAPaid, AbiValue.Uint(id)),
                result => cart.lineAPaid = new AbiReader(result).Bool(0), Warn);

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigActiveHero, AbiValue.Uint(id)),
                result => cart.activeHeroId = new AbiReader(result).Bytes32(0), Warn);

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigHeroIds, AbiValue.Uint(id)),
                result =>
                {
                    var reader = new AbiReader(result);
                    if (!reader.IsEmpty) cart.heroIds = reader.Bytes32ArrayAt(0);
                }, Warn);

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigPocketBalance, AbiValue.Uint(id), AbiValue.Bytes32(GhstCurrency)),
                result => cart.pocketGhstWei = new AbiReader(result).Uint(0).ToString(), Warn);

            yield return rpc.EthCall(ChainConfig.CartridgeDiamond,
                Abi.EncodeCall(SigGetCheckpoint, AbiValue.Uint(id)),
                result =>
                {
                    var outer = new AbiReader(result);
                    if (outer.IsEmpty) return;
                    var tuple = outer.Follow(0);           // struct is returned behind a pointer
                    cart.checkpoint = new CartridgeCheckpoint
                    {
                        nonce = (int)tuple.Uint(0),
                        stateHash = tuple.Bytes32(1),
                        stateUri = tuple.StringAt(2),
                        savedAt = (long)tuple.Uint(3),
                    };
                }, Warn);

            done?.Invoke(cart);
        }

        static void Warn(string message) => Debug.LogWarning("[Cartridge] " + message);

        // ------------------------------------------------------------ writes

        IEnumerator Send(string to, string data, BigInteger value, Action<string> done, Action<string> fail)
        {
            if (!CanWrite)
            {
                fail?.Invoke("Read only: no wallet in this build.");
                yield break;
            }

            var wallet = WalletBridge.Ensure();
            string switchError = null;
            yield return wallet.SwitchChain(null, e => switchError = e);
            // a wallet already on the right chain, or one that refuses the
            // request, still gets a shot at the transaction itself
            if (switchError != null) Debug.LogWarning("[Cartridge] switch chain: " + switchError);

            string hash = null;
            string sendError = null;
            yield return wallet.SendTransaction(signer, to, data, value, h => hash = h, e => sendError = e);
            if (sendError != null) { fail?.Invoke(sendError); yield break; }

            string receiptError = null;
            yield return wallet.WaitForReceipt(hash, null, e => receiptError = e);
            if (receiptError != null) { fail?.Invoke(receiptError); yield break; }

            done?.Invoke(hash);
        }

        public IEnumerator Mint(string owner, Action<string> done, Action<string> fail)
        {
            var data = Abi.EncodeCall(SigMintCartridge,
                AbiValue.Bytes32(ChainConfig.GameIdHash), AbiValue.Address(owner));
            yield return Send(ChainConfig.ConsoleDiamond, data, BigInteger.Zero, done, fail);
        }

        public IEnumerator PayLineA(CartridgeSnapshot cart, Action<string> done, Action<string> fail)
        {
            var id = BigInteger.Parse(cart.cartridgeId);
            var data = Abi.EncodeCall(SigPayLineA, AbiValue.Uint(id));
            yield return Send(ChainConfig.CartridgeDiamond, data, LineAFeeWei(), done, fail);
        }

        /// The diamond has no getter for a cartridge's Line A fee, so the amount
        /// is configurable and shown to the player before they sign.
        public static BigInteger LineAFeeWei()
        {
            var raw = PlayerPrefs.GetString("chain.lineAWei", "0");
            return BigInteger.TryParse(raw, out var wei) ? wei : BigInteger.Zero;
        }

        public IEnumerator BindOwned(CartridgeSnapshot cart, string sourceTokenId, Action<string> done, Action<string> fail)
        {
            if (!BigInteger.TryParse(sourceTokenId, out var token))
            {
                fail?.Invoke("Bad gotchi token id.");
                yield break;
            }
            var data = Abi.EncodeCall(SigBindOwned,
                AbiValue.Uint(BigInteger.Parse(cart.cartridgeId)), AbiValue.Uint(token));
            yield return Send(ChainConfig.CartridgeDiamond, data, BigInteger.Zero, done, fail);
        }

        public IEnumerator BindStarter(CartridgeSnapshot cart, string templateId, Action<string> done, Action<string> fail)
        {
            var data = Abi.EncodeCall(SigBindStarter,
                AbiValue.Uint(BigInteger.Parse(cart.cartridgeId)),
                AbiValue.Bytes32(Keccak.HashHex(templateId)),
                AbiValue.Address("0x0000000000000000000000000000000000000000"));
            yield return Send(ChainConfig.CartridgeDiamond, data, StarterBindFeeWei, done, fail);
        }

        public IEnumerator SaveCheckpoint(CartridgeSnapshot cart, BomberGhstSave save, string label,
            Action<string> done, Action<string> fail)
        {
            var payload = JsonUtility.ToJson(save);
            var stateHash = Keccak.HashHex(payload);
            var uri = string.IsNullOrEmpty(label)
                ? $"aarcade://bomberghst/{cart.cartridgeId}/{cart.checkpoint.nonce + 1}"
                : label;

            var data = Abi.EncodeCall(SigCheckpointSave,
                AbiValue.Uint(BigInteger.Parse(cart.cartridgeId)),
                AbiValue.Bytes32(stateHash),
                AbiValue.Str(uri));
            yield return Send(ChainConfig.CartridgeDiamond, data, BigInteger.Zero, done, fail);
        }

        /// The message the web SDK signs for a checkpoint, kept identical so a
        /// checkpoint made here verifies the same way there.
        public static string CheckpointMessage(string cartridgeId, int nextNonce, string stateHash) =>
            $"Aarcade cartridge checkpoint\nCartridge: {cartridgeId}\nNonce: {nextNonce}\nState: {stateHash}";
    }
}
