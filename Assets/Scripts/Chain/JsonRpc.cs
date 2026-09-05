using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BomberGhst.Chain
{
    /// Read-only JSON-RPC over HTTP. Works in the editor and in WebGL (the
    /// public Base endpoints send permissive CORS headers), so every view call
    /// goes through here rather than through the wallet.
    public class JsonRpc : MonoBehaviour
    {
        public static JsonRpc I { get; private set; }

        int nextId = 1;

        public static JsonRpc Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("JsonRpc");
            DontDestroyOnLoad(go);
            I = go.AddComponent<JsonRpc>();
            return I;
        }

        [Serializable]
        class RpcError
        {
            public int code;
            public string message;
        }

        [Serializable]
        class RpcResponse
        {
            public string result;
            public RpcError error;
        }

        /// eth_call against a contract; hands back the raw 0x result.
        public IEnumerator EthCall(string to, string data, Action<string> onResult, Action<string> onError)
        {
            string body =
                "{\"jsonrpc\":\"2.0\",\"id\":" + (nextId++) +
                ",\"method\":\"eth_call\",\"params\":[{\"to\":\"" + to + "\",\"data\":\"" + data + "\"},\"latest\"]}";
            yield return Post(body, onResult, onError);
        }

        public IEnumerator Send(string method, string paramsJson, Action<string> onResult, Action<string> onError)
        {
            string body =
                "{\"jsonrpc\":\"2.0\",\"id\":" + (nextId++) +
                ",\"method\":\"" + method + "\",\"params\":" + (paramsJson ?? "[]") + "}";
            yield return Post(body, onResult, onError);
        }

        IEnumerator Post(string body, Action<string> onResult, Action<string> onError)
        {
            using var req = new UnityWebRequest(ChainConfig.Rpc, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 20;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error ?? "network error");
                yield break;
            }

            RpcResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<RpcResponse>(req.downloadHandler.text);
            }
            catch (Exception e)
            {
                onError?.Invoke("bad rpc response: " + e.Message);
                yield break;
            }

            if (parsed == null) { onError?.Invoke("empty rpc response"); yield break; }
            if (parsed.error != null && !string.IsNullOrEmpty(parsed.error.message))
            {
                onError?.Invoke(parsed.error.message);
                yield break;
            }
            onResult?.Invoke(parsed.result ?? "0x");
        }
    }
}
