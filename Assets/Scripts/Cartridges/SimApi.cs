using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BomberGhst.Cartridges
{
    /// HTTP client for the Aarcade cartridge SIM. On the Aarcade site the game
    /// is same origin, so the relative root goes through the Vercel rewrite to
    /// the home tunnel; outside the browser we hit that host directly. Note the
    /// rewrite strips the /api/cartridge-sim prefix, which is why the two bases
    /// differ.
    public static class SimApi
    {
        public const string RelativeRoot = "/api/cartridge-sim";
        public const string DirectRoot = "https://cartridge.aarcadeghst.com";

        /// PlayerPrefs "cartridge.api" overrides both, for pointing at a laptop
        /// or a different tunnel.
        public static string Root
        {
            get
            {
                var custom = PlayerPrefs.GetString("cartridge.api", null);
                if (!string.IsNullOrWhiteSpace(custom)) return custom.TrimEnd('/');
#if UNITY_WEBGL && !UNITY_EDITOR
                return RelativeRoot;
#else
                return DirectRoot;
#endif
            }
        }

        public static string Url(string path) => Root + path;

        // --------------------------------------------------------------- dtos

        [Serializable]
        public class SimLineA
        {
            public bool required;
            public bool paid;
            public string amountGhst;
        }

        [Serializable]
        public class SimEntitlement
        {
            public SimLineA lineA;
        }

        [Serializable]
        public class SimGotchi
        {
            public string bindType;
            public string sourceTokenId;
            public string collateral;
            public int hauntId;
        }

        [Serializable]
        public class SimPocket
        {
            public string ghst;
            public string usdc;
        }

        [Serializable]
        public class SimCheckpoint
        {
            public int nonce;
            public string hash;
        }

        [Serializable]
        public class SimSnapshot
        {
            public string cartridgeId;
            public string gameId;
            public string owner;
            public string originConsole;
            public string activeCAavegotchiId;
            public SimGotchi cAavegotchi;
            public SimPocket pocket;
            public SimCheckpoint checkpoint;
            public SimEntitlement entitlement;
        }

        [Serializable]
        public class SimList
        {
            public SimSnapshot[] cartridges;
        }

        [Serializable]
        class SimError
        {
            public string error;
        }

        // -------------------------------------------------------------- verbs

        public static IEnumerator Get(string path, Action<string> onJson, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(Url(path));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 20;
            yield return req.SendWebRequest();
            Finish(req, onJson, onError);
        }

        public static IEnumerator Post(string path, string body, Action<string> onJson, Action<string> onError)
        {
            using var req = new UnityWebRequest(Url(path), UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body ?? "{}"));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;
            yield return req.SendWebRequest();
            Finish(req, onJson, onError);
        }

        static void Finish(UnityWebRequest req, Action<string> onJson, Action<string> onError)
        {
            var text = req.downloadHandler != null ? req.downloadHandler.text : null;
            if (req.result == UnityWebRequest.Result.Success)
            {
                onJson?.Invoke(text);
                return;
            }

            // the SIM reports failures as {"error":"..."} with a status code
            string message = null;
            if (!string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
            {
                try { message = JsonUtility.FromJson<SimError>(text)?.error; }
                catch { /* fall through to the transport error */ }
            }
            onError?.Invoke(string.IsNullOrEmpty(message)
                ? $"{req.error} ({req.responseCode})"
                : message);
        }

        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        /// The SIM reports pocket balances in whole tokens; the rest of the game
        /// keeps wei so one field serves both back ends.
        public static string ToWei(string wholeTokens)
        {
            if (string.IsNullOrWhiteSpace(wholeTokens)) return "0";
            var parts = wholeTokens.Trim().Split('.');
            if (!System.Numerics.BigInteger.TryParse(parts[0], out var whole)) return "0";
            var wei = whole * System.Numerics.BigInteger.Pow(10, 18);
            if (parts.Length > 1)
            {
                var frac = (parts[1] + new string('0', 18)).Substring(0, 18);
                if (System.Numerics.BigInteger.TryParse(frac, out var fracWei)) wei += fracWei;
            }
            return wei.ToString();
        }
    }
}
