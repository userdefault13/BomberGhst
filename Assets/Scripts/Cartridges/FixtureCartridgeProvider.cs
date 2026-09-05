using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BomberGhst.Cartridges
{
    /// Offline stand-in so the cartridge screen is usable in the editor, on a
    /// desktop build, or when the RPC is unreachable. Mutations are kept in
    /// persistentDataPath rather than pretending to reach a chain.
    public class FixtureCartridgeProvider : ICartridgeProvider
    {
        [Serializable]
        class Fixture
        {
            public List<CartridgeSnapshot> cartridges = new List<CartridgeSnapshot>();
        }

        const string ResourceName = "BomberGhstCartridges";

        Fixture data;

        public string Label => "local fixture";
        public bool CanWrite => true;

        string SavePath => Path.Combine(Application.persistentDataPath, "bomberghst-cartridges.json");

        void EnsureLoaded()
        {
            if (data != null) return;

            if (File.Exists(SavePath))
            {
                try
                {
                    data = JsonUtility.FromJson<Fixture>(File.ReadAllText(SavePath));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Cartridge] fixture reload failed: " + e.Message);
                }
            }

            if (data == null)
            {
                var asset = Resources.Load<TextAsset>(ResourceName);
                if (asset != null) data = JsonUtility.FromJson<Fixture>(asset.text);
            }

            data ??= new Fixture();
        }

        void Persist()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
            catch (Exception e) { Debug.LogWarning("[Cartridge] fixture save failed: " + e.Message); }
        }

        CartridgeSnapshot Find(string cartridgeId)
        {
            EnsureLoaded();
            return data.cartridges.Find(c => c.cartridgeId == cartridgeId);
        }

        public IEnumerator LoadForPlayer(string owner, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            EnsureLoaded();
            var found = data.cartridges.Find(c =>
                string.Equals(c.owner, owner, StringComparison.OrdinalIgnoreCase));
            if (found == null && data.cartridges.Count > 0) found = data.cartridges[0];
            done?.Invoke(found ?? CartridgeSnapshot.Missing(owner));
            yield break;
        }

        public IEnumerator LoadById(string cartridgeId, Action<CartridgeSnapshot> done, Action<string> fail)
        {
            done?.Invoke(Find(cartridgeId) ?? CartridgeSnapshot.Missing(null));
            yield break;
        }

        public IEnumerator Mint(string owner, Action<string> done, Action<string> fail)
        {
            EnsureLoaded();
            var cart = new CartridgeSnapshot
            {
                cartridgeId = (data.cartridges.Count + 1).ToString(),
                owner = owner,
                exists = true,
                lineAPaid = true,
            };
            data.cartridges.Add(cart);
            Persist();
            done?.Invoke("local");
            yield break;
        }

        public IEnumerator PayLineA(CartridgeSnapshot cart, Action<string> done, Action<string> fail)
        {
            var target = Find(cart.cartridgeId);
            if (target != null) { target.lineAPaid = true; Persist(); }
            done?.Invoke("local");
            yield break;
        }

        public IEnumerator BindOwned(CartridgeSnapshot cart, string sourceTokenId, Action<string> done, Action<string> fail)
        {
            var target = Find(cart.cartridgeId);
            if (target != null)
            {
                target.activeHeroId = "owned-" + sourceTokenId;
                target.heroLabel = "owned #" + sourceTokenId;
                target.heroIds.Add(target.activeHeroId);
                Persist();
            }
            done?.Invoke("local");
            yield break;
        }

        public IEnumerator BindStarter(CartridgeSnapshot cart, string templateId, Action<string> done, Action<string> fail)
        {
            var target = Find(cart.cartridgeId);
            if (target != null)
            {
                target.activeHeroId = "starter-" + templateId + "-" + target.heroIds.Count;
                // the SIM defaults a starter bind to dai collateral
                target.heroCollateral = "dai";
                target.heroHaunt = 1;
                target.heroLabel = "dai starter";
                target.heroIds.Add(target.activeHeroId);
                Persist();
            }
            done?.Invoke("local");
            yield break;
        }

        public IEnumerator SaveCheckpoint(CartridgeSnapshot cart, BomberGhstSave save, string label,
            Action<string> done, Action<string> fail)
        {
            var target = Find(cart.cartridgeId);
            if (target != null)
            {
                target.checkpoint.nonce += 1;
                target.checkpoint.stateHash = Chain.Keccak.HashHex(JsonUtility.ToJson(save));
                target.checkpoint.stateUri = string.IsNullOrEmpty(label)
                    ? $"local://bomberghst/{cart.cartridgeId}/{target.checkpoint.nonce}"
                    : label;
                target.checkpoint.savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Persist();
            }
            done?.Invoke("local");
            yield break;
        }
    }
}
