using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace BomberGhst.Chain
{
    /// Signing side of the chain layer. Reads go over plain JSON-RPC, but
    /// anything that needs a signature is handed to the page's provider:
    /// window.AarcadeChain.provider if the Aarcade shell injected one for the
    /// embed, otherwise window.ethereum. Outside WebGL there is no signer, so
    /// the screens fall back to read-only and say so.
    public class WalletBridge : MonoBehaviour
    {
        public static WalletBridge I { get; private set; }

        [Serializable]
        class ChainReply
        {
            public string id;
            public bool ok;
            public string value;
            public string error;
        }

        class Pending
        {
            public bool done;
            public bool ok;
            public string value;
            public string error;
        }

        readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>();
        int nextId = 1;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern int AarcadeChain_HasProvider();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void AarcadeChain_Request(string id, string method, string paramsJson, string go, string cb);
#endif

        public static WalletBridge Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("WalletBridge");
            DontDestroyOnLoad(go);
            I = go.AddComponent<WalletBridge>();
            return I;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public static bool HasProvider
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return AarcadeChain_HasProvider() == 1; }
                catch { return false; }
#else
                return false;
#endif
            }
        }

        /// The one place JS calls back into.
        public void OnChainResult(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            ChainReply reply;
            try { reply = JsonUtility.FromJson<ChainReply>(json); }
            catch (Exception e) { Debug.LogWarning("[WalletBridge] bad reply: " + e.Message); return; }
            if (reply == null || !pending.TryGetValue(reply.id, out var slot)) return;

            slot.ok = reply.ok;
            slot.value = reply.value;
            slot.error = reply.error;
            slot.done = true;
        }

        IEnumerator Request(string method, string paramsJson, Action<string> onOk, Action<string> onError)
        {
            if (!HasProvider)
            {
                onError?.Invoke("No wallet in this build. Open the game inside the Aarcade shell to sign.");
                yield break;
            }

            string id = "r" + (nextId++);
            var slot = new Pending();
            pending[id] = slot;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                AarcadeChain_Request(id, method, paramsJson ?? "[]", gameObject.name, nameof(OnChainResult));
            }
            catch (Exception e)
            {
                pending.Remove(id);
                onError?.Invoke(e.Message);
                yield break;
            }
#endif

            float deadline = Time.realtimeSinceStartup + 180f;   // wallet prompts are slow
            while (!slot.done && Time.realtimeSinceStartup < deadline) yield return null;
            pending.Remove(id);

            if (!slot.done) { onError?.Invoke("Wallet request timed out."); yield break; }
            if (!slot.ok) { onError?.Invoke(string.IsNullOrEmpty(slot.error) ? "Wallet rejected." : slot.error); yield break; }
            onOk?.Invoke(slot.value);
        }

        public IEnumerator RequestAccounts(Action<string> onAddress, Action<string> onError)
        {
            string found = null;
            yield return Request("eth_requestAccounts", "[]", v => found = FirstAddress(v), onError);
            if (found == null) yield break;
            if (string.IsNullOrEmpty(found)) { onError?.Invoke("Wallet returned no account."); yield break; }
            onAddress?.Invoke(found);
        }

        public IEnumerator ChainId(Action<int> onChainId, Action<string> onError)
        {
            yield return Request("eth_chainId", "[]",
                v => onChainId?.Invoke((int)Hex.ToBigInteger(v)), onError);
        }

        /// Asks the wallet to move to the cartridge chain before a write.
        public IEnumerator SwitchChain(Action onDone, Action<string> onError)
        {
            string p = "[{\"chainId\":\"" + ChainConfig.ChainIdHex + "\"}]";
            yield return Request("wallet_switchEthereumChain", p, _ => onDone?.Invoke(), onError);
        }

        /// personal_sign, used for SIM checkpoints. The message is sent as hex
        /// so wallets do not have to guess at the encoding.
        public IEnumerator PersonalSign(string message, string address,
            Action<string> onSignature, Action<string> onError)
        {
            var hex = Hex.Encode(System.Text.Encoding.UTF8.GetBytes(message ?? string.Empty));
            string p = "[\"" + hex + "\",\"" + address + "\"]";
            yield return Request("personal_sign", p, onSignature, onError);
        }

        public IEnumerator SendTransaction(string from, string to, string data, BigInteger value,
            Action<string> onTxHash, Action<string> onError)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[{\"from\":\"").Append(from).Append("\",\"to\":\"").Append(to).Append('"');
            if (!string.IsNullOrEmpty(data)) sb.Append(",\"data\":\"").Append(data).Append('"');
            if (!value.IsZero) sb.Append(",\"value\":\"").Append(Hex.Quantity(value)).Append('"');
            sb.Append("}]");
            yield return Request("eth_sendTransaction", sb.ToString(), onTxHash, onError);
        }

        /// Polls until the transaction is mined, so the UI can refresh after.
        public IEnumerator WaitForReceipt(string txHash, Action onMined, Action<string> onError)
        {
            var rpc = JsonRpc.Ensure();
            float deadline = Time.realtimeSinceStartup + 120f;
            while (Time.realtimeSinceStartup < deadline)
            {
                bool got = false;
                string err = null;
                yield return rpc.Send("eth_getTransactionReceipt", "[\"" + txHash + "\"]",
                    result => got = !string.IsNullOrEmpty(result) && result != "null" && result.Length > 4,
                    e => err = e);
                if (err != null) { onError?.Invoke(err); yield break; }
                if (got) { onMined?.Invoke(); yield break; }
                yield return new WaitForSecondsRealtime(2f);
            }
            onError?.Invoke("Timed out waiting for the transaction.");
        }

        static string FirstAddress(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            int start = value.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return string.Empty;
            return value.Substring(start, Math.Min(42, value.Length - start));
        }
    }
}
