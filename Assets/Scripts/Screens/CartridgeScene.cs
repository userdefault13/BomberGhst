using System;
using System.Collections.Generic;
using BomberGhst.Cartridges;
using BomberGhst.Chain;
using UnityEngine;

namespace BomberGhst.Screens
{
    /// Cartridge management: what the connected wallet owns for this game, and
    /// the operations the cartridge diamond exposes - mint, pay line A, bind a
    /// hero, checkpoint the save, then insert it and play.
    public class CartridgeScene : RetroScreen
    {
        const int DetailRows = 8;

        readonly Label[] keys = new Label[DetailRows];
        readonly Label[] values = new Label[DetailRows];

        SpriteRenderer cartArt;
        Label cartId, header, status, hint, entryLabel;
        MenuList menu;
        CartridgeService service;

        bool entering;              // typing a gotchi token id
        string entryBuffer = "";
        float t;

        protected override void Build()
        {
            Sfx.Ensure();

            header = Text("Header", 0f, 100f, 1f, 20);
            header.Set("CARTRIDGE MANAGEMENT", Pal.Yellow);

            var rule = Sprite("Rule", Art.Blank(), 0f, 91f, 9);
            rule.transform.localScale = new Vector3(280f / Config.PPU, 1f / Config.PPU, 1f);
            rule.color = new Color(0.32f, 0.28f, 0.55f, 1f);

            cartArt = Sprite("Cart", Art.CartridgeShell(0, false), -112f, 34f, 15);
            cartArt.transform.localScale = Vector3.one * 1.6f;
            cartId = Text("CartId", -112f, -22f, 1f, 20);

            for (int i = 0; i < DetailRows; i++)
            {
                float y = 74f - i * 11f;
                keys[i] = TextLeft("Key" + i, -62f, y, 1f, 20);
                values[i] = TextLeft("Val" + i, 10f, y, 1f, 20);
            }

            menu = MenuList.Create(transform, 0f, -32f, 9f, 8, 20);

            entryLabel = Text("Entry", 0f, -34f, 1f, 22);
            entryLabel.SetVisible(false);

            status = Text("Status", 0f, -102f, 1f, 20);
            hint = Text("Hint", 0f, -112f, 1f, 20);
            hint.SetVisible(false);
        }

        void Start()
        {
            service = CartridgeService.Ensure();
            service.OnChanged += Rebuild;

            if (!service.Connected)
            {
                SceneFlow.ToTitle();
                return;
            }

            if (service.Active == null) service.Refresh();
            Rebuild();
        }

        void OnDestroy()
        {
            if (service != null) service.OnChanged -= Rebuild;
        }

        // ----------------------------------------------------------------- ui

        void Rebuild()
        {
            var cart = service.Active;
            bool has = cart != null && cart.exists;
            bool busy = service.Busy;

            cartArt.sprite = Art.CartridgeShell(0, has && cart.HasHero);
            cartArt.color = has ? Color.white : new Color(0.4f, 0.4f, 0.45f, 1f);
            cartId.Set(has ? "CARTRIDGE #" + cart.cartridgeId : "NO CARTRIDGE",
                has ? (Color32)Pal.White : (Color32)Pal.Gray);

            Row(0, "GAME", ChainConfig.GameId.ToUpperInvariant(), Pal.Bone);
            // only name the chain when the chain is actually answering
            if (service.Source == CartridgeSource.Sim)
                Row(1, "HOST", HostLabel(), Pal.Bone);
            else
                Row(1, "CHAIN", ChainConfig.Network.ToUpperInvariant(), Pal.Bone);
            Row(2, "SOURCE", CartridgeService.SourceName(service.Source), SourceTint(service.Source));
            Row(3, "OWNER", Hex.Shorten(service.Address, 6, 4).ToUpperInvariant(), Pal.Bone);

            if (has)
            {
                Row(4, "LINE A", cart.lineAPaid ? "PAID" : "UNPAID",
                    cart.lineAPaid ? (Color32)Pal.Green : (Color32)Pal.Red);
                Row(5, "HERO", HeroText(cart), cart.HasHero ? (Color32)Pal.Green : (Color32)Pal.Gray);
                Row(6, "POCKET", cart.PocketGhstShort() + " GHST", Pal.Cyan);
                Row(7, "SAVE", cart.checkpoint != null && cart.checkpoint.nonce > 0
                    ? "#" + cart.checkpoint.nonce + " " + Hex.Shorten(cart.checkpoint.stateHash, 6, 4).ToUpperInvariant()
                    : "NONE", Pal.Bone);
            }
            else
            {
                Row(4, "LINE A", "-", Pal.Gray);
                Row(5, "HERO", "-", Pal.Gray);
                Row(6, "POCKET", "-", Pal.Gray);
                Row(7, "SAVE", "-", Pal.Gray);
            }

            var items = new List<MenuItem>();

            if (!has)
                items.Add(new MenuItem("MINT CARTRIDGE", () => service.Mint(), !busy));

            if (has && !cart.lineAPaid)
                items.Add(new MenuItem("PAY LINE A", () => service.PayLineA(), !busy));

            if (has && cart.lineAPaid && !cart.HasHero)
            {
                items.Add(new MenuItem("BIND STARTER GOTCHI", () => service.BindStarter(), !busy));
                items.Add(new MenuItem("BIND OWNED GOTCHI", BeginEntry, !busy));
            }

            if (has)
                items.Add(new MenuItem("SAVE CHECKPOINT", () => service.SaveCheckpoint(), !busy && cart.lineAPaid));

            items.Add(new MenuItem("INSERT AND PLAY", SceneFlow.ToMatch, service.ReadyToPlay || has));
            items.Add(new MenuItem("SOURCE  " + CartridgeService.SourceName(service.Source) + "  >",
                () => service.CycleSource(), !busy));
            items.Add(new MenuItem("REFRESH", () => service.Refresh(), !busy));
            items.Add(new MenuItem("BACK", SceneFlow.ToTitle));

            menu.Set(items);
        }

        static string HostLabel()
        {
            var root = Cartridges.SimApi.Root;
            int scheme = root.IndexOf("//", StringComparison.Ordinal);
            if (scheme >= 0) root = root.Substring(scheme + 2);
            return root.ToUpperInvariant();
        }

        static Color32 SourceTint(CartridgeSource source) => source switch
        {
            CartridgeSource.Sim => Pal.Cyan,
            CartridgeSource.Chain => Pal.Green,
            _ => Pal.Gray,
        };

        static string HeroText(CartridgeSnapshot cart)
        {
            if (!cart.HasHero) return "NONE";
            if (!string.IsNullOrEmpty(cart.heroLabel)) return cart.heroLabel.ToUpperInvariant();
            return Hex.Shorten(cart.activeHeroId, 6, 4).ToUpperInvariant();
        }

        void Row(int i, string key, string value, Color32 tint)
        {
            keys[i].Set(key, Pal.Gray);
            values[i].Set(value, tint);
        }

        // -------------------------------------------------------- token entry

        void BeginEntry()
        {
            entering = true;
            entryBuffer = "";
            entryLabel.SetVisible(true);
            hint.SetVisible(true);
            hint.Set("DIGITS THEN ENTER   ESC CANCELS", Pal.Gray);
        }

        void EndEntry(bool submit)
        {
            entering = false;
            entryLabel.SetVisible(false);
            hint.SetVisible(false);
            if (submit && entryBuffer.Length > 0) service.BindOwned(entryBuffer);
            Rebuild();
        }

        void TickEntry()
        {
            foreach (char c in Input.inputString)
            {
                if (c >= '0' && c <= '9' && entryBuffer.Length < 12) entryBuffer += c;
                else if ((c == '\b') && entryBuffer.Length > 0)
                    entryBuffer = entryBuffer.Substring(0, entryBuffer.Length - 1);
            }

            if (Pressed(KeyCode.Escape)) { EndEntry(false); return; }
            if (Pressed(KeyCode.Return, KeyCode.KeypadEnter)) { EndEntry(true); return; }

            bool blink = Mathf.Sin(t * 8f) > 0f;
            entryLabel.Set("GOTCHI ID  " + entryBuffer + (blink ? "_" : " "), Pal.Yellow);
        }

        void Update()
        {
            t += Time.deltaTime;

            if (entering) { TickEntry(); }
            else
            {
                menu.Tick();
                if (Pressed(KeyCode.Escape)) SceneFlow.ToTitle();
            }

            // the cartridge id already sits under the artwork, so only show the
            // status line when it is saying something else
            var cart = service.Active;
            string line = service.Status ?? "";
            if (!service.Busy && cart != null && cart.exists &&
                line.Equals("Cartridge #" + cart.cartridgeId, StringComparison.OrdinalIgnoreCase))
                line = "";
            status.Set(line.ToUpperInvariant(), service.Busy ? (Color32)Pal.Yellow : (Color32)Pal.Bone);

            // a bound cartridge gets a gentle idle tilt
            float wobble = Mathf.Sin(t * 1.6f) * 2f;
            cartArt.transform.rotation = Quaternion.Euler(0f, 0f, wobble);
        }
    }
}
