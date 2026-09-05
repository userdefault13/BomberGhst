using System;
using System.Collections;
using BomberGhst.Chain;
using UnityEngine;

namespace BomberGhst.Cartridges
{
    /// The cartridge SIM: the backend that mints and holds cartridges today,
    /// ahead of the move onto Base Sepolia. It owns the minter authority, so
    /// unlike the chain path a player really can create their own cartridge
    /// here - the session token is the credential.
    public class SimCartridgeProvider : ICartridgeProvider
    {
        readonly string sessionToken;
        readonly string signer;

        public SimCartridgeProvider(string signerAddress, string sessionToken)
        {
            signer = signerAddress;
            this.sessionToken = sessionToken ?? string.Empty;
        }

        public string Label => "sim";
        public bool CanWrite => true;

        // -------------------------------------------------------------- reads

        public IEnumerator LoadForPlayer(string owner, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            if (string.IsNullOrEmpty(owner)) { fail?.Invoke("No wallet address."); yield break; }

            string path = "/cartridges?owner=" + UnityWebRequestEscape(owner) +
                          "&gameId=" + UnityWebRequestEscape(ChainConfig.GameId);
            bool handled = false;

            yield return SimApi.Get(path,
                json =>
                {
                    var list = JsonUtility.FromJson<SimApi.SimList>(json);
                    var match = Pick(list);
                    done?.Invoke(match != null ? Map(match) : CartridgeSnapshot.Missing(owner));
                    handled = true;
                },
                error => { fail?.Invoke(error); handled = true; });

            if (!handled) fail?.Invoke("No response from the cartridge SIM.");
        }

        static SimApi.SimSnapshot Pick(SimApi.SimList list)
        {
            if (list?.cartridges == null) return null;
            foreach (var c in list.cartridges)
                if (c != null && string.Equals(c.gameId, ChainConfig.GameId, StringComparison.OrdinalIgnoreCase))
                    return c;
            return null;
        }

        public IEnumerator LoadById(string cartridgeId, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            if (string.IsNullOrEmpty(cartridgeId) || cartridgeId == "0")
            {
                done?.Invoke(CartridgeSnapshot.Missing(signer));
                yield break;
            }

            yield return SimApi.Get("/cartridges/" + UnityWebRequestEscape(cartridgeId),
                json => done?.Invoke(Map(JsonUtility.FromJson<SimApi.SimSnapshot>(json))),
                fail);
        }

        static CartridgeSnapshot Map(SimApi.SimSnapshot sim)
        {
            if (sim == null || string.IsNullOrEmpty(sim.cartridgeId))
                return CartridgeSnapshot.Missing(null);

            var lineA = sim.entitlement?.lineA;
            var cart = new CartridgeSnapshot
            {
                cartridgeId = sim.cartridgeId,
                gameId = string.IsNullOrEmpty(sim.gameId) ? ChainConfig.GameId : sim.gameId,
                owner = sim.owner,
                exists = true,
                // no entitlement block means the game carries no line A fee
                lineAPaid = lineA == null || !lineA.required || lineA.paid,
                pocketGhstWei = SimApi.ToWei(sim.pocket?.ghst),
            };

            if (!string.IsNullOrEmpty(sim.activeCAavegotchiId))
                cart.activeHeroId = sim.activeCAavegotchiId;
            else if (sim.cAavegotchi != null && !string.IsNullOrEmpty(sim.cAavegotchi.sourceTokenId))
                cart.activeHeroId = sim.cAavegotchi.bindType + "-" + sim.cAavegotchi.sourceTokenId;

            if (sim.cAavegotchi != null)
            {
                cart.heroLabel = string.IsNullOrEmpty(sim.cAavegotchi.sourceTokenId)
                    ? (sim.cAavegotchi.bindType ?? "BOUND")
                    : sim.cAavegotchi.bindType + " #" + sim.cAavegotchi.sourceTokenId;
                cart.heroIds.Add(cart.activeHeroId);
            }

            if (sim.checkpoint != null)
            {
                cart.checkpoint = new CartridgeCheckpoint
                {
                    nonce = sim.checkpoint.nonce,
                    stateHash = string.IsNullOrEmpty(sim.checkpoint.hash) ? "0x0" : sim.checkpoint.hash,
                };
            }

            return cart;
        }

        // ------------------------------------------------------------- writes

        public IEnumerator Mint(string owner, Action<string> done, Action<string> fail)
        {
            string body = "{\"owner\":\"" + SimApi.Escape(owner) +
                          "\",\"gameId\":\"" + SimApi.Escape(ChainConfig.GameId) +
                          "\",\"sessionToken\":\"" + SimApi.Escape(sessionToken) + "\"}";
            yield return SimApi.Post("/cartridges/ensure", body, _ => done?.Invoke("sim"), fail);
        }

        public IEnumerator PayLineA(CartridgeSnapshot cart, Action<string> done, Action<string> fail)
        {
            string body = "{\"sessionToken\":\"" + SimApi.Escape(sessionToken) + "\",\"simPay\":true}";
            yield return SimApi.Post("/cartridges/" + UnityWebRequestEscape(cart.cartridgeId) + "/pay-line-a",
                body, _ => done?.Invoke("sim"), fail);
        }

        public IEnumerator BindOwned(CartridgeSnapshot cart, string sourceTokenId, Action<string> done, Action<string> fail)
        {
            string body = "{\"sourceTokenId\":\"" + SimApi.Escape(sourceTokenId) +
                          "\",\"sessionToken\":\"" + SimApi.Escape(sessionToken) + "\"}";
            yield return SimApi.Post("/cartridges/" + UnityWebRequestEscape(cart.cartridgeId) + "/bind-owned",
                body, _ => done?.Invoke("sim"), fail);
        }

        public IEnumerator BindStarter(CartridgeSnapshot cart, string templateId, Action<string> done, Action<string> fail)
        {
            string body = "{\"templateId\":\"" + SimApi.Escape(templateId) +
                          "\",\"collateral\":\"dai\",\"simPay\":true,\"sessionToken\":\"" +
                          SimApi.Escape(sessionToken) + "\"}";
            yield return SimApi.Post("/cartridges/" + UnityWebRequestEscape(cart.cartridgeId) + "/bind-starter",
                body, _ => done?.Invoke("sim"), fail);
        }

        /// The SIM rebuilds this message from the state it receives and checks
        /// the recovered signer against the cartridge owner, so the hash, the
        /// message and the transmitted state all have to line up.
        public IEnumerator SaveCheckpoint(CartridgeSnapshot cart, BomberGhstSave save, string label,
            Action<string> done, Action<string> fail)
        {
            int nextNonce = (cart.checkpoint?.nonce ?? 0) + 1;
            string stateHash = CheckpointCrypto.HashGameState(save);
            string message = CheckpointCrypto.SimMessage(cart.cartridgeId, nextNonce, stateHash);

            if (!WalletBridge.HasProvider)
            {
                fail?.Invoke("Checkpoints need the wallet to sign - open the game in the Aarcade shell.");
                yield break;
            }

            string signature = null;
            string signError = null;
            yield return WalletBridge.Ensure().PersonalSign(message, signer,
                s => signature = s, e => signError = e);

            if (signError != null || string.IsNullOrEmpty(signature))
            {
                fail?.Invoke(signError ?? "Wallet did not sign the checkpoint.");
                yield break;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("{\"sessionToken\":\"").Append(SimApi.Escape(sessionToken)).Append('"');
            sb.Append(",\"gameState\":").Append(JsonUtility.ToJson(save));
            sb.Append(",\"signature\":\"").Append(SimApi.Escape(signature)).Append('"');
            sb.Append(",\"message\":\"").Append(SimApi.Escape(message)).Append('"');
            if (!string.IsNullOrEmpty(label))
                sb.Append(",\"label\":\"").Append(SimApi.Escape(label)).Append('"');
            sb.Append('}');

            yield return SimApi.Post("/cartridges/" + UnityWebRequestEscape(cart.cartridgeId) + "/checkpoint",
                sb.ToString(), _ => done?.Invoke("sim"), fail);
        }

        static string UnityWebRequestEscape(string value) =>
            UnityEngine.Networking.UnityWebRequest.EscapeURL(value ?? string.Empty);
    }
}
