using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace BomberGhst.Aarcade
{
    /// Receives the Aarcade GameViewer session when BomberGhst is embedded as a
    /// WebGL console game. The payload shape and the SendMessage entry point
    /// match the other Aarcade titles, so the existing shell needs no changes:
    /// it either calls SetSession on this GameObject or parks the JSON on
    /// window.__AARCADE_PENDING_SESSION before the player boots.
    public class AarcadeBridge : MonoBehaviour
    {
        public static AarcadeBridge Instance { get; private set; }

        const int PendingSessionBufferSize = 16 * 1024;

        static string pendingSessionJson;
        static string lastAppliedSessionId;
        static GameObject bootstrapGo;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void WebGL_GetPendingAarcadeSession(byte[] buffer, int bufferSize);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void AarcadeNotifyWalletReady(string walletAddress);
#endif

        [Serializable]
        public class SessionPayload
        {
            public string sessionToken;
            public string sessionId;
            public string gameId;
            public string playerId;      // wallet address of the signed in player
            public string gameType;
            public string cartridgeId;
            public bool cartridgeSim;
        }

        public SessionPayload CurrentSession { get; private set; }
        public string SessionToken => CurrentSession?.sessionToken;
        public string CartridgeId => CurrentSession?.cartridgeId;
        public string PlayerAddress => CurrentSession?.playerId;
        public bool CartridgeSim => CurrentSession != null && CurrentSession.cartridgeSim;
        public bool HasSession => CurrentSession != null && !string.IsNullOrEmpty(CurrentSession.playerId);

        public event Action<SessionPayload> OnSessionReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureExists()
        {
            if (Instance != null || bootstrapGo != null) return;
            bootstrapGo = new GameObject("AarcadeBridge");
            bootstrapGo.AddComponent<AarcadeBridge>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryApplyPendingSession();
        }

        void Start() => StartCoroutine(PollPendingSession());

        /// The shell sometimes finishes authenticating after the player boots.
        IEnumerator PollPendingSession()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (CurrentSession != null) yield break;
                TryApplyPendingSession();
            }
        }

        void TryApplyPendingSession()
        {
            if (!string.IsNullOrEmpty(pendingSessionJson))
            {
                var json = pendingSessionJson;
                pendingSessionJson = null;
                SetSession(json);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var buffer = new byte[PendingSessionBufferSize];
                WebGL_GetPendingAarcadeSession(buffer, buffer.Length);
                var json = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                if (!string.IsNullOrWhiteSpace(json)) SetSession(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AarcadeBridge] Pending session read failed: " + e.Message);
            }
#endif
        }

        /// Called from the Aarcade GameViewer via SendMessage.
        public void SetSession(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            if (Instance == null) { pendingSessionJson = json; return; }

            try
            {
                ApplySession(JsonUtility.FromJson<SessionPayload>(json));
            }
            catch (Exception e)
            {
                Debug.LogError("[AarcadeBridge] SetSession failed: " + e.Message);
            }
        }

        public void ApplySession(SessionPayload payload)
        {
            if (payload == null) return;
            if (!string.IsNullOrEmpty(payload.sessionId) &&
                string.Equals(lastAppliedSessionId, payload.sessionId, StringComparison.Ordinal))
                return;

            lastAppliedSessionId = payload.sessionId;
            CurrentSession = payload;
            Debug.Log($"[AarcadeBridge] Session ready for {payload.gameType} ({payload.sessionId})");

            OnSessionReady?.Invoke(payload);
            NotifyWalletReady(payload.playerId);
        }

        /// Editor and standalone convenience: pretend a shell signed us in.
        public void ApplyLocalSession(string address)
        {
            ApplySession(new SessionPayload
            {
                sessionId = "local-" + DateTime.UtcNow.Ticks,
                sessionToken = string.Empty,
                gameId = Chain.ChainConfig.GameId,
                gameType = Chain.ChainConfig.GameId,
                playerId = address,
                cartridgeSim = false,
            });
        }

        public void NotifyWalletReady(string walletAddress)
        {
            if (string.IsNullOrEmpty(walletAddress)) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            AarcadeNotifyWalletReady(walletAddress);
#else
            Debug.Log("[AarcadeBridge] Wallet ready: " + walletAddress);
#endif
        }
    }
}
