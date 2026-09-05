using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    /// Status bar, the two side panels with player cards, and all the
    /// full-screen callouts. Everything is sprites drawn in the pixel font.
    public class Hud : MonoBehaviour
    {
        class Card
        {
            public GameObject Root;
            public SpriteRenderer Icon;
            public Label Name, Bombs, Fire, Speed, Kick;
            public SpriteRenderer[] Pips;
        }

        /// Aavegotchi portraits are 32px; the drawn ghost is 16px.
        static float IconScale => GotchiArt.Available ? 0.6f : 1f;

        GameDirector game;
        Label roundLabel, timeLabel, targetLabel;
        Label bigLabel, smallLabel, flashLabel;
        SpriteRenderer dim;
        readonly List<Card> cards = new List<Card>();
        readonly List<GameObject> titleBits = new List<GameObject>();
        Label titleHumans, titleBots;
        float flashUntil;

        public static Hud Create(Transform parent, GameDirector game)
        {
            var go = new GameObject("Hud");
            go.transform.SetParent(parent, false);
            var hud = go.AddComponent<Hud>();
            hud.game = game;
            hud.Build();
            return hud;
        }

        /// Screen pixel coordinates (origin at screen centre, y up) to world.
        static Vector3 P(float px, float py) => new Vector3(px / Config.PPU, Config.CamY + py / Config.PPU, 0f);

        SpriteRenderer Sprite(string name, Sprite sprite, Vector3 pos, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        Label MakeLabel(string name, Vector3 pos, float scale, int order, Transform parent = null)
        {
            var lb = Label.Create(parent != null ? parent : transform, name, order, scale);
            lb.transform.position = pos;
            return lb;
        }

        void Build()
        {
            Sprite("PanelL", Art.Panel(true), P(-(Config.ViewW - Config.PanelW) * 0.5f, 0f), Config.SortPanel);
            Sprite("PanelR", Art.Panel(false), P((Config.ViewW - Config.PanelW) * 0.5f, 0f), Config.SortPanel);
            Sprite("Bar", Art.Bar(), P(0f, (Config.ViewH - Config.BarH) * 0.5f), Config.SortPanel + 1);

            roundLabel = MakeLabel("Round", P(-124f, 104f), 1f, Config.SortHud);
            timeLabel = MakeLabel("Time", P(0f, 104f), 1f, Config.SortHud);
            targetLabel = MakeLabel("Target", P(120f, 104f), 1f, Config.SortHud);

            float[] cardY = { 58f, -36f };
            for (int slot = 0; slot < 4; slot++)
            {
                float x = (slot % 2 == 0) ? -(Config.ViewW - Config.PanelW) * 0.5f : (Config.ViewW - Config.PanelW) * 0.5f;
                float y = cardY[slot / 2];
                cards.Add(BuildCard(slot, x, y));
            }

            dim = Sprite("Dim", Art.Blank(), P(0f, 0f), Config.SortHud + 8);
            dim.transform.localScale = new Vector3(Config.ViewW / (float)Config.PPU, Config.ViewH / (float)Config.PPU, 1f);
            dim.color = new Color(0.02f, 0.01f, 0.06f, 0.72f);
            dim.enabled = false;

            bigLabel = MakeLabel("Big", P(0f, 14f), 3f, Config.SortHud + 10);
            smallLabel = MakeLabel("Small", P(0f, -14f), 1f, Config.SortHud + 10);
            flashLabel = MakeLabel("Flash", P(0f, 78f), 2f, Config.SortHud + 10);
            bigLabel.SetVisible(false);
            smallLabel.SetVisible(false);
            flashLabel.SetVisible(false);

            BuildTitle();
        }

        Card BuildCard(int slot, float x, float y)
        {
            var root = new GameObject("Card" + slot);
            root.transform.SetParent(transform, false);
            var c = new Card { Root = root };
            var t = root.transform;

            c.Name = MakeLabel("Name", P(x, y + 30f), 1f, Config.SortHud, t);
            c.Icon = Sprite("Icon", Art.Ghost(slot, Facing.Down, 0),
                P(x, y + 13f - GotchiArt.CenterOffsetUnits * Config.PPU * IconScale), Config.SortHud, t);
            c.Icon.transform.localScale = Vector3.one * IconScale;
            c.Bombs = MakeLabel("Bombs", P(x, y - 3f), 1f, Config.SortHud, t);
            c.Fire = MakeLabel("Fire", P(x, y - 12f), 1f, Config.SortHud, t);
            c.Speed = MakeLabel("Speed", P(x, y - 21f), 1f, Config.SortHud, t);
            c.Kick = MakeLabel("Kick", P(x, y - 30f), 1f, Config.SortHud, t);

            c.Pips = new SpriteRenderer[Config.WinsNeeded];
            for (int i = 0; i < c.Pips.Length; i++)
            {
                float px = x + (i - (c.Pips.Length - 1) * 0.5f) * 7f;
                var pip = Sprite("Pip", Art.Blank(), P(px, y - 40f), Config.SortHud, t);
                pip.transform.localScale = Vector3.one * (5f / Config.PPU);
                c.Pips[i] = pip;
            }
            return c;
        }

        void BuildTitle()
        {
            void Add(GameObject go) => titleBits.Add(go);

            var logo = MakeLabel("Logo", P(0f, 66f), 3f, Config.SortHud + 12);
            logo.Set("BOMBER GHST", Pal.Yellow);
            Add(logo.gameObject);

            var sub = MakeLabel("Sub", P(0f, 44f), 1f, Config.SortHud + 12);
            sub.Set("A NEO GEO STYLE BLAST PARTY", Pal.Cyan);
            Add(sub.gameObject);

            titleHumans = MakeLabel("Humans", P(0f, 12f), 1f, Config.SortHud + 12);
            Add(titleHumans.gameObject);
            titleBots = MakeLabel("Bots", P(0f, -2f), 1f, Config.SortHud + 12);
            Add(titleBots.gameObject);

            var start = MakeLabel("Start", P(0f, -24f), 1f, Config.SortHud + 12);
            start.Set("PRESS ENTER TO FIGHT", Pal.Yellow);
            Add(start.gameObject);

            string[] help = {
                "P1  WASD  +  SPACE",
                "P2  ARROWS  +  RIGHT SHIFT",
                "ESC  BACK TO CARTRIDGES",
            };
            for (int i = 0; i < help.Length; i++)
            {
                var lb = MakeLabel("Help" + i, P(0f, -50f - i * 11f), 1f, Config.SortHud + 12);
                lb.Set(help[i], Pal.Bone);
                Add(lb.gameObject);
            }
        }

        // --------------------------------------------------------------- public

        public void ShowTitle(bool on)
        {
            foreach (var go in titleBits) go.SetActive(on);
            dim.enabled = on;
            foreach (var c in cards) c.Root.SetActive(!on);
            if (on) HideCenter();
        }

        public void ShowCenter(string big, string small)
        {
            bigLabel.SetVisible(true);
            bigLabel.Set(big, Pal.White);
            bool hasSmall = !string.IsNullOrEmpty(small);
            smallLabel.SetVisible(hasSmall);
            if (hasSmall) smallLabel.Set(small, Pal.Yellow);
        }

        public void HideCenter()
        {
            bigLabel.SetVisible(false);
            smallLabel.SetVisible(false);
        }

        public void Flash(string text)
        {
            flashLabel.Set(text, Pal.Red);
            flashLabel.SetVisible(true);
            flashUntil = Time.time + 2f;
        }

        public void Refresh()
        {
            if (game.State == GameState.Title)
            {
                titleHumans.Set("PLAYERS   < " + game.HumanCount + " >", Pal.White);
                titleBots.Set("BOTS   ^ " + game.BotCount + " ~", Pal.White);
                var svc = Cartridges.CartridgeService.I;
                roundLabel.Set(svc != null && svc.HasCartridge
                    ? "CART #" + svc.Active.cartridgeId
                    : (svc != null && svc.IsGuest ? "GUEST" : ""), Pal.Cyan);
                timeLabel.Set("BOMBER GHST", Pal.Bone);
                targetLabel.Set("ESC BACK", Pal.Gray);
                return;
            }

            roundLabel.Set("ROUND " + game.Round, Pal.Bone);
            targetLabel.Set("WIN " + Config.WinsNeeded, Pal.Bone);

            int secs = Mathf.Max(0, Mathf.CeilToInt(game.TimeLeft));
            var timeColor = game.SuddenDeath ? Pal.Red : (secs <= 10 ? Pal.Yellow : (Color32)Pal.White);
            timeLabel.Set(game.SuddenDeath ? "SUDDEN DEATH" : $"{secs / 60}:{secs % 60:00}", timeColor);

            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                bool active = i < game.Bombers.Count;
                c.Root.SetActive(active);
                if (!active) continue;

                var b = game.Bombers[i];
                c.Name.Set(b.Name, b.Alive ? Pal.Team[i] : Pal.Gray);
                c.Icon.color = b.Alive ? Color.white : new Color(0.35f, 0.35f, 0.4f, 1f);
                c.Bombs.Set("B " + b.BombLimit, Pal.Bone);
                c.Fire.Set("F " + b.Power, Pal.Bone);
                c.Speed.Set("S " + (b.SpeedLevel + 1), Pal.Bone);
                c.Kick.Set(b.HasKick ? "KICK" : "----", b.HasKick ? Pal.Green : (Color32)Pal.Gray);
                for (int p = 0; p < c.Pips.Length; p++)
                    c.Pips[p].color = p < b.Wins ? (Color)(Color32)Pal.Team[i] : new Color(1f, 1f, 1f, 0.15f);
            }

            if (flashUntil > 0f && Time.time > flashUntil)
            {
                flashUntil = 0f;
                flashLabel.SetVisible(false);
            }
        }
    }
}
