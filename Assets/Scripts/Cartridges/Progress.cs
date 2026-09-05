using System;
using UnityEngine;

namespace BomberGhst.Cartridges
{
    /// Local play record. This is what a checkpoint actually commits, so the
    /// on-chain state hash means something.
    public static class Progress
    {
        const string Key = "bomberghst.save";

        static BomberGhstSave cached;

        public static BomberGhstSave Current
        {
            get
            {
                if (cached != null) return cached;
                var raw = PlayerPrefs.GetString(Key, null);
                if (!string.IsNullOrEmpty(raw))
                {
                    try { cached = JsonUtility.FromJson<BomberGhstSave>(raw); }
                    catch { cached = null; }
                }
                return cached ??= new BomberGhstSave();
            }
        }

        public static void Save()
        {
            Current.lastPlayedUtc = DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static void RecordMatch(bool won)
        {
            Current.matchesPlayed++;
            if (won) Current.matchesWon++;
            Save();
        }

        public static void RecordRoundWin() { Current.roundsWon++; Save(); }
        public static void RecordBomb() => Current.bombsPlaced++;
        public static void RecordSuddenDeath() { Current.suddenDeaths++; Save(); }
    }
}
