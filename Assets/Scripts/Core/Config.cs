using UnityEngine;

namespace BomberGhst
{
    /// Global tuning + the fixed Neo-Geo style presentation constants.
    /// The Neo Geo runs 320x224; at 16px tiles that is a 15x13 playfield
    /// (240x208) with a 16px status bar on top and a 40px panel each side.
    public static class Config
    {
        public const int PPU = 16;              // pixels per unit / tile size
        public const int W = 15;                // playfield tiles, including the wall ring
        public const int H = 13;
        public const int ViewW = 320;
        public const int ViewH = 224;

        public const int PanelW = (ViewW - W * PPU) / 2;   // 40px side panels
        public const int BarH = ViewH - H * PPU;           // 16px top bar

        // Camera is centred so the arena sits low and the bar occupies the top unit.
        public const float CamY = 0.5f;
        public const float OrthoSize = ViewH * 0.5f / PPU; // 7

        // --- gameplay tuning -------------------------------------------------
        public const float BaseSpeed = 3.6f;    // tiles / second
        public const float SpeedStep = 0.55f;
        public const float MaxSpeed = 6.9f;
        public const float BodyHalf = 0.34f;    // collision half-extent
        public const float FuseTime = 2.4f;
        public const float FlameLife = 0.42f;
        public const float KickSpeed = 7.5f;
        public const int StartBombs = 1;
        public const int StartPower = 2;
        public const int MaxBombs = 8;
        public const int MaxPower = 9;
        public const float SoftFill = 0.82f;    // density of destructible blocks
        public const float RoundTime = 99f;
        public const float SuddenDeathStep = 0.28f;
        public const int WinsNeeded = 3;

        // --- sorting ---------------------------------------------------------
        public const int SortFloor = -100;
        public const int SortPower = -20;
        public const int SortBlock = 0;         // + y sort
        public const int SortActor = 0;         // + y sort
        public const int SortFlame = 400;
        public const int SortPanel = 800;
        public const int SortHud = 900;

        /// y-sorting so things lower on the screen draw in front.
        public static int YSort(float tileY, int bias = 0)
        {
            return Mathf.RoundToInt((H - tileY) * 4f) + bias;
        }

        public static Vector3 TileToWorld(float tx, float ty, float z = 0f)
        {
            return new Vector3(tx - (W - 1) * 0.5f, ty - (H - 1) * 0.5f, z);
        }
    }

    public static class Pal
    {
        public static Color32 Rgb(uint hex) => new Color32(
            (byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF), 255);

        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        public static readonly Color32 Ink = Rgb(0x12101A);   // outline
        public static readonly Color32 Night = Rgb(0x1B1830);
        public static readonly Color32 Steel = Rgb(0x5A6480);
        public static readonly Color32 SteelHi = Rgb(0x8E9BB8);
        public static readonly Color32 SteelLo = Rgb(0x333B54);
        public static readonly Color32 Brick = Rgb(0xA9603C);
        public static readonly Color32 BrickHi = Rgb(0xD08A57);
        public static readonly Color32 BrickLo = Rgb(0x6E3822);
        public static readonly Color32 GrassA = Rgb(0x2E7A46);
        public static readonly Color32 GrassB = Rgb(0x27683C);
        public static readonly Color32 GrassHi = Rgb(0x3C9455);
        public static readonly Color32 White = Rgb(0xFFFFFF);
        public static readonly Color32 Bone = Rgb(0xE8E4F0);
        public static readonly Color32 Gray = Rgb(0x7A8090);
        public static readonly Color32 Yellow = Rgb(0xFFE04A);
        public static readonly Color32 Orange = Rgb(0xFF8C1A);
        public static readonly Color32 Red = Rgb(0xE23B3B);
        public static readonly Color32 Blue = Rgb(0x3B7BE2);
        public static readonly Color32 Cyan = Rgb(0x4FE0E0);
        public static readonly Color32 Green = Rgb(0x46C24A);
        public static readonly Color32 Purple = Rgb(0xA24BE2);
        public static readonly Color32 Panel = Rgb(0x201C38);
        public static readonly Color32 PanelHi = Rgb(0x3A3466);

        /// The four bomber colours: body, shade, highlight.
        public static readonly Color32[] Team = {
            Rgb(0xE2495B), Rgb(0x4A8CF0), Rgb(0x4CC85C), Rgb(0xF0C63A),
        };
        public static readonly Color32[] TeamDark = {
            Rgb(0x8E2338), Rgb(0x24479B), Rgb(0x1F7A34), Rgb(0x9A7412),
        };
        public static readonly Color32[] TeamLight = {
            Rgb(0xFF8FA0), Rgb(0x9CC4FF), Rgb(0x9BE8A2), Rgb(0xFFE9A0),
        };
        public static readonly string[] TeamName = { "P1", "P2", "P3", "P4" };
    }
}
