using System.Collections.Generic;
using BomberGhst.Aarcade;
using BomberGhst.Cartridges;
using BomberGhst.Chain;
using UnityEngine;

namespace BomberGhst.Screens
{
    /// Attract screen and wallet gate. The Aarcade shell normally hands us a
    /// session before this is even drawn; when it has not, the player can ask
    /// for the page wallet or skip the whole thing and play as a guest.
    public class TitleScene : RetroScreen
    {
        Label statusLabel, walletLabel, chainLabel, hintLabel;
        MenuList menu;
        readonly List<SpriteRenderer> ghosts = new List<SpriteRenderer>();
        CartridgeService service;
        float t;

        protected override void Build()
        {
            Sfx.Ensure();

            var logo = Text("Logo", 0f, 74f, 3f, 20);
            logo.Set("BOMBER GHST", Pal.Yellow);

            var sub = Text("Sub", 0f, 54f, 1f, 20);
            sub.Set("AARCADE CARTRIDGE EDITION", Pal.Cyan);

            for (int i = 0; i < 4; i++)
            {
                var g = Sprite("Ghost" + i, Art.Ghost(i, Facing.Down, 0), -60f + i * 40f, 26f, 15);
                g.transform.localScale = Vector3.one * 1.5f;
                ghosts.Add(g);
            }

            // session panel
            var panel = Sprite("Panel", Art.Blank(), 0f, -12f, 8);
            panel.transform.localScale = new Vector3(232f / Config.PPU, 40f / Config.PPU, 1f);
            panel.color = new Color(0.06f, 0.05f, 0.14f, 0.9f);
            var edge = Sprite("PanelEdge", Art.Blank(), 0f, -12f, 7);
            edge.transform.localScale = new Vector3(236f / Config.PPU, 44f / Config.PPU, 1f);
            edge.color = new Color(0.32f, 0.28f, 0.55f, 1f);

            walletLabel = Text("Wallet", 0f, -2f, 1f, 20);
            statusLabel = Text("Status", 0f, -14f, 1f, 20);
            chainLabel = Text("Chain", 0f, -25f, 1f, 20);

            menu = MenuList.Create(transform, 0f, -52f, 12f, 4, 20);

            hintLabel = Text("Hint", 0f, -100f, 1f, 20);
            hintLabel.Set("UP DOWN SELECT    ENTER CONFIRM", Pal.Gray);
        }

        void Start()
        {
            service = CartridgeService.Ensure();
            service.OnChanged += Rebuild;

            if (DemoMode.AutoPlay)
            {
                SceneFlow.ToMatch();
                return;
            }
            Rebuild();
        }

        void OnDestroy()
        {
            if (service != null) service.OnChanged -= Rebuild;
        }

        void Rebuild()
        {
            var items = new List<MenuItem>();

            if (service.Connected)
            {
                items.Add(new MenuItem("CARTRIDGES", SceneFlow.ToCartridges));
                items.Add(new MenuItem("QUICK MATCH", SceneFlow.ToMatch));
                items.Add(new MenuItem("DISCONNECT", () => service.Disconnect()));
            }
            else
            {
                items.Add(new MenuItem("CONNECT WALLET", () => service.ConnectWallet(), !service.Busy));
                items.Add(new MenuItem("PLAY AS GUEST", () =>
                {
                    service.PlayAsGuest();
                    SceneFlow.ToMatch();
                }));
            }

            menu.Set(items);
        }

        void Update()
        {
            t += Time.deltaTime;
            menu.Tick();

            for (int i = 0; i < ghosts.Count; i++)
            {
                float bob = Mathf.Sin(t * 2.2f + i * 0.8f) * 3f;
                ghosts[i].transform.position = P(-60f + i * 40f, 26f + bob);
                ghosts[i].sprite = Art.Ghost(i, Facing.Down, Mathf.FloorToInt(t * 4f + i) % 4);
            }

            var bridge = AarcadeBridge.Instance;
            if (service.Connected)
                walletLabel.Set("WALLET " + Hex.Shorten(service.Address, 6, 4).ToUpperInvariant(), Pal.Green);
            else if (bridge != null && bridge.HasSession)
                walletLabel.Set("AARCADE SESSION FOUND", Pal.Cyan);
            else
                walletLabel.Set("NO WALLET CONNECTED", Pal.Bone);

            statusLabel.Set(string.IsNullOrEmpty(service.Status) ? "READY" : service.Status.ToUpperInvariant(),
                service.Busy ? (Color32)Pal.Yellow : (Color32)Pal.Bone);

            chainLabel.Set(ChainConfig.Network.ToUpperInvariant() + "  " + ChainConfig.ChainId, Pal.Gray);

            if (Pressed(KeyCode.Escape) && service.Connected) service.Disconnect();
        }
    }
}
