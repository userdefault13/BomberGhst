using System.Collections.Generic;
using BomberGhst.Cartridges;
using BomberGhst.Screens;
using UnityEngine;

namespace BomberGhst
{
    public enum GameState { Title, Intro, Play, Paused, RoundOver, MatchOver }

    /// Owns the match: builds the scene, runs the round loop, sudden death and
    /// the win conditions.
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector I { get; private set; }

        public Arena Arena { get; private set; }
        public readonly List<Bomber> Bombers = new List<Bomber>();
        public GameState State { get; private set; } = GameState.Title;
        public bool AcceptsInput => State == GameState.Play;

        public int Round { get; private set; }
        public float TimeLeft { get; private set; }
        public bool SuddenDeath { get; private set; }

        int humanCount = 1;
        int botCount = 3;

        Hud hud;
        Camera cam;
        float phaseTimer;
        float resolveTimer = -1f;
        int lastTickSecond;
        List<Vector2Int> spiral;
        int spiralIndex;
        float spiralTimer;
        Bomber roundWinner;
        Bomber champion;

        static readonly Vector2Int[] Spawns = {
            new Vector2Int(1, Arena.H - 2),          // P1 top-left
            new Vector2Int(Arena.W - 2, 1),          // P2 bottom-right
            new Vector2Int(Arena.W - 2, Arena.H - 2),// P3 top-right
            new Vector2Int(1, 1),                    // P4 bottom-left
        };

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;

            BuildCamera();
            Arena = new GameObject("Arena").AddComponent<Arena>();
            Arena.transform.SetParent(transform, false);
            Sfx.Ensure();
            hud = Hud.Create(transform, this);
            EnterTitle();
            if (DemoMode.AutoPlay)
            {
                hud.ShowTitle(false);
                StartMatch();
            }
        }

        void BuildCamera()
        {
            var go = new GameObject("PixelCamera") { tag = "MainCamera" };
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, Config.CamY, -10f);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Config.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Night;
            cam.nearClipPlane = -1f;
            cam.farClipPlane = 50f;
            go.AddComponent<Letterbox>();
            go.AddComponent<AudioListener>();
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Title: TickTitle(); break;
                case GameState.Intro: TickIntro(); break;
                case GameState.Play: TickPlay(); break;
                case GameState.Paused: TickPaused(); break;
                case GameState.RoundOver:
                case GameState.MatchOver: TickPost(); break;
            }

            if (Application.isFocused && Down(KeyCode.Escape)) ToggleQuit();
            hud.Refresh();
        }

        // ---------------------------------------------------------------- pause

        /// Escape pauses first and only quits on a second press, so a stray
        /// key (or the window losing focus) cannot throw away a match.
        void ToggleQuit()
        {
            if (State == GameState.Play) Pause();
            else if (State == GameState.Paused) EnterTitle();
            else if (State == GameState.Title) BackToShell();
        }

        /// From the match setup screen, Escape goes back out to the front end.
        void BackToShell()
        {
            if (DemoMode.AutoPlay) return;
            var svc = CartridgeService.I;
            if (svc != null && svc.HasCartridge) SceneFlow.ToCartridges();
            else SceneFlow.ToTitle();
        }

        void Pause()
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            Sfx.Music(false);
            hud.ShowCenter("PAUSED", "ENTER RESUME   ESC QUIT");
        }

        void Resume()
        {
            Time.timeScale = 1f;
            if (State == GameState.Paused)
            {
                State = GameState.Play;
                hud.HideCenter();
                Sfx.Music(true);
            }
        }

        void TickPaused()
        {
            if (Down(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space)) Resume();
        }

        // ---------------------------------------------------------------- title

        void EnterTitle()
        {
            Time.timeScale = 1f;
            State = GameState.Title;
            Arena.Clear();
            foreach (var b in Bombers) if (b != null) Destroy(b.gameObject);
            Bombers.Clear();
            champion = null;
            roundWinner = null;
            Round = 0;
            Sfx.Music(false);
            hud.ShowTitle(true);
        }

        void TickTitle()
        {
            int h = humanCount, b = botCount;
            if (Down(KeyCode.RightArrow, KeyCode.D)) h++;
            if (Down(KeyCode.LeftArrow, KeyCode.A)) h--;
            if (Down(KeyCode.UpArrow, KeyCode.W)) b++;
            if (Down(KeyCode.DownArrow, KeyCode.S)) b--;

            h = Mathf.Clamp(h, 1, 2);
            b = Mathf.Clamp(b, 0, 3);
            if (h + b > 4) b = 4 - h;
            if (h + b < 2) b = 2 - h;

            if (h != humanCount || b != botCount)
            {
                humanCount = h; botCount = b;
                Sfx.Play(Sound.Menu);
            }

            if (Down(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space))
            {
                hud.ShowTitle(false);
                StartMatch();
            }
        }

        static bool Down(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        public int HumanCount => humanCount;
        public int BotCount => botCount;

        // ---------------------------------------------------------------- match

        void StartMatch()
        {
            Round = 0;
            SpawnBombers();
            Sfx.Music(true);
            NextRound();
        }

        void SpawnBombers()
        {
            foreach (var b in Bombers) if (b != null) Destroy(b.gameObject);
            Bombers.Clear();

            int total = DemoMode.AutoPlay ? 4 : Mathf.Clamp(humanCount + botCount, 2, 4);
            for (int i = 0; i < total; i++)
            {
                var go = new GameObject("Bomber" + (i + 1));
                go.transform.SetParent(transform, false);
                var bomber = go.AddComponent<Bomber>();
                bool human = !DemoMode.AutoPlay && i < humanCount;
                IBrain brain = human
                    ? (i == 0 ? (IBrain)HumanBrain.Wasd(humanCount == 1) : HumanBrain.Arrows())
                    : new BotBrain { Skill = Mathf.Lerp(0.6f, 1f, i / 3f) };
                bomber.Init(Arena, i, Pal.TeamName[i], brain, Spawns[i]);
                Bombers.Add(bomber);
            }
        }

        void NextRound()
        {
            Round++;
            SuddenDeath = false;
            TimeLeft = DemoMode.RoundTime;
            lastTickSecond = Mathf.CeilToInt(TimeLeft);
            resolveTimer = -1f;
            roundWinner = null;
            spiral = Arena.SpiralOrder();
            spiralIndex = 0;
            spiralTimer = 0f;

            var used = new List<Vector2Int>();
            for (int i = 0; i < Bombers.Count; i++) used.Add(Spawns[i]);
            Arena.Build(Random.Range(1, 999999), used);
            for (int i = 0; i < Bombers.Count; i++) Bombers[i].ResetForRound(Spawns[i]);

            State = GameState.Intro;
            phaseTimer = 1.9f;
            hud.ShowCenter("ROUND " + Round, "READY");
        }

        void TickIntro()
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer < 0.65f) hud.ShowCenter("GO!", "");
            if (phaseTimer <= 0f)
            {
                hud.HideCenter();
                State = GameState.Play;
            }
        }

        void TickPlay()
        {
            TimeLeft -= Time.deltaTime;

            int sec = Mathf.CeilToInt(Mathf.Max(TimeLeft, 0f));
            if (sec != lastTickSecond)
            {
                lastTickSecond = sec;
                if (sec <= 10 && sec > 0) Sfx.Play(Sound.Tick);
            }

            if (TimeLeft <= 0f)
            {
                if (!SuddenDeath)
                {
                    SuddenDeath = true;
                    hud.Flash("SUDDEN DEATH");
                    if (!DemoMode.AutoPlay) Progress.RecordSuddenDeath();
                }
                TickSpiral();
            }

            if (resolveTimer > 0f)
            {
                resolveTimer -= Time.deltaTime;
                if (resolveTimer <= 0f) ResolveRound();
            }
        }

        void TickSpiral()
        {
            spiralTimer -= Time.deltaTime;
            if (spiralTimer > 0f || spiral == null || spiralIndex >= spiral.Count) return;
            spiralTimer = Config.SuddenDeathStep;

            var cell = spiral[spiralIndex++];
            Arena.DropBlock(cell.x, cell.y);
            foreach (var b in Bombers)
                if (b.Alive && b.TilePos == cell) b.Die();
        }

        public void OnBomberDied(Bomber who)
        {
            if (State != GameState.Play) return;
            // Give simultaneous deaths a beat to land so double KOs read as draws.
            if (resolveTimer < 0f) resolveTimer = 0.9f;
        }

        void ResolveRound()
        {
            resolveTimer = -1f;
            var alive = Bombers.FindAll(b => b.Alive);
            if (alive.Count > 1) return;

            roundWinner = alive.Count == 1 ? alive[0] : null;
            if (roundWinner != null)
            {
                roundWinner.Wins++;
                if (roundWinner.Wins >= Config.WinsNeeded) champion = roundWinner;
                if (!DemoMode.AutoPlay && roundWinner.Slot == 0) Progress.RecordRoundWin();
            }
            if (champion != null && !DemoMode.AutoPlay) Progress.RecordMatch(champion.Slot == 0);

            Sfx.Play(Sound.Fanfare);
            State = champion != null ? GameState.MatchOver : GameState.RoundOver;
            phaseTimer = champion != null ? 4.5f : 2.8f;

            if (champion != null)
            {
                Sfx.Music(false);
                hud.ShowCenter(champion.Name + " WINS!", "PRESS ENTER");
            }
            else
            {
                hud.ShowCenter(roundWinner != null ? roundWinner.Name + " TAKES IT" : "DRAW",
                    roundWinner != null ? "ROUND " + Round : "NOBODY LEFT");
            }
        }

        void TickPost()
        {
            phaseTimer -= Time.deltaTime;
            bool go = Down(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space);
            if (phaseTimer > 0f && !go) return;

            if (State != GameState.MatchOver) { NextRound(); return; }
            if (DemoMode.AutoPlay) { StartMatch(); return; }
            EnterTitle();
        }
    }
}
