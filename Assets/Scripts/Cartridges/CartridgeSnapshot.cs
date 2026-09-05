using System;
using System.Collections.Generic;
using BomberGhst.Chain;

namespace BomberGhst.Cartridges
{
    /// A cartridge as the game cares about it. Mirrors the fields the Aarcade
    /// cartridge diamond exposes through its view functions.
    [Serializable]
    public class CartridgeSnapshot
    {
        public string cartridgeId = "0";
        public string gameId = ChainConfig.GameId;
        public string gameIdHash;
        public string owner;
        public bool exists;
        public bool lineAPaid;
        public string activeHeroId;
        public string heroLabel;
        public string pocketGhstWei = "0";
        public CartridgeCheckpoint checkpoint = new CartridgeCheckpoint();
        public List<string> heroIds = new List<string>();

        public bool HasHero => !string.IsNullOrEmpty(activeHeroId) && !Hex.IsZeroWord(activeHeroId);
        public bool IsPlayable => exists && lineAPaid;

        public static CartridgeSnapshot Missing(string owner) =>
            new CartridgeSnapshot { owner = owner, exists = false, cartridgeId = "0" };

        /// GHST in the cartridge pocket, trimmed to something a 40px panel can show.
        public string PocketGhstShort()
        {
            if (string.IsNullOrEmpty(pocketGhstWei)) return "0";
            if (!System.Numerics.BigInteger.TryParse(pocketGhstWei, out var wei)) return "0";
            var whole = wei / System.Numerics.BigInteger.Pow(10, 18);
            var frac = wei % System.Numerics.BigInteger.Pow(10, 18) / System.Numerics.BigInteger.Pow(10, 16);
            return frac.IsZero ? whole.ToString() : whole + "." + frac.ToString("00");
        }
    }

    [Serializable]
    public class CartridgeCheckpoint
    {
        public int nonce;
        public string stateHash = "0x0";
        public string stateUri = "";
        public long savedAt;
    }

    /// The BomberGhst save payload that gets hashed into a checkpoint.
    [Serializable]
    public class BomberGhstSave
    {
        public int schemaVersion = 1;
        public int matchesPlayed;
        public int roundsWon;
        public int matchesWon;
        public int bombsPlaced;
        public int suddenDeaths;
        public string lastPlayedUtc;
    }
}
